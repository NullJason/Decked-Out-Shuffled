using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

public class GenericNodeEditor : EditorWindow
{
    private NodeTree currentTree;
    private INodeTree nodeTreeInterface;
    private List<INode> nodes => nodeTreeInterface?.Nodes;
    private Dictionary<INode, Vector2> achievementPortPositions = new Dictionary<INode, Vector2>();
    private Dictionary<AchievementNodeDrawer, Rect> achievementDrawerPortRects = new Dictionary<AchievementNodeDrawer, Rect>();
    private GUIStyle nodeStyle;
    private GUIStyle selectedNodeStyle;
    private GUIStyle connectedPortStyle;
    private GUIStyle disconnectedPortStyle;
    private GUIStyle nodeHeaderStyle;
    private GUIStyle resizeStyle;
    
    private INode selectedNode;
    private INode connectingFrom;
    private Vector2 panOffset;
    private bool isPanning;
    private Vector2 lastMousePosition;
    
    // Zoom functionality
    private float zoomLevel = 1.0f;
    private const float MIN_ZOOM = 0.3f;
    private const float MAX_ZOOM = 2.0f;
    private Vector2 zoomPan = Vector2.zero;
    
    // Node resizing
    private INode resizingNode;
    private Vector2 resizeStart;
    private Rect resizeStartRect;
    
    // Node dragging
    private INode draggingNode;
    private Vector2 dragOffset;
    
    // Node type management
    private Dictionary<System.Type, INodeDrawer> nodeDrawers = new Dictionary<System.Type, INodeDrawer>();
    
    // Style initialization flag
    private bool stylesInitialized = false;
    
    // Title bar height for dragging
    private const float TITLE_BAR_HEIGHT = 25f;
    
    [MenuItem("Window/Generic Node Editor")]
    public static void OpenWindow()
    {
        GetWindow<GenericNodeEditor>("Node Editor");
    }

    private void OnEnable()
    {
        // var dialogueDrawer = new DialogueINodeDrawer();
    //     dialogueDrawer.OnChoicePortClicked = (node, choiceIndex) => {
    //     connectingFrom = node;
    //     // Store the choice index for connection
    // };
        // Register node drawers for different types
        // nodeDrawers[typeof(DialogueINode)] = new DialogueINodeDrawer();
         var achievementDrawer = new AchievementNodeDrawer();
        achievementDrawer.OnChoicePortClicked = (node, choiceIndex) => {
            connectingFrom = node;
        };
        nodeDrawers[typeof(AchievementNode)] = achievementDrawer;
    }

    private void InitializeStyles()
    {
        if (stylesInitialized) return;

        // Node styles
        nodeStyle = new GUIStyle();
        // nodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
        if (nodeStyle.normal.background == null)
            nodeStyle.normal.background = CreateColorTexture(new Color(0.3f, 0.3f, 0.3f, 0.6f));
        nodeStyle.border = new RectOffset(12, 12, 12, 12);
        nodeStyle.padding = new RectOffset(10, 10, 10, 10);
        
        selectedNodeStyle = new GUIStyle();
        // selectedNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1 on.png") as Texture2D;
        if (selectedNodeStyle.normal.background == null)
            selectedNodeStyle.normal.background = CreateColorTexture(new Color(0.043f, 0.271f, 0.588f, 0.8f));
        selectedNodeStyle.border = new RectOffset(12, 12, 12, 12);
        selectedNodeStyle.padding = new RectOffset(10, 10, 10, 10);

        // Node header style
        nodeHeaderStyle = new GUIStyle(EditorStyles.label);
        nodeHeaderStyle.alignment = TextAnchor.MiddleCenter;
        nodeHeaderStyle.fontStyle = FontStyle.Bold;
        nodeHeaderStyle.normal.textColor = Color.white;
        nodeHeaderStyle.normal.background = CreateColorTexture(new Color(0.2f, 0.2f, 0.2f, 1f));

        // Port styles - different colors for connected vs disconnected
        connectedPortStyle = new GUIStyle();
        connectedPortStyle.normal.background = CreateColorTexture(new Color(0f, 0.5f, 0f, 1f)); // Dark green
        connectedPortStyle.fixedWidth = 16f;
        connectedPortStyle.fixedHeight = 16f;

        disconnectedPortStyle = new GUIStyle();
        disconnectedPortStyle.normal.background = CreateColorTexture(new Color(0.5f, 0f, 0f, 1f)); // Dark red
        disconnectedPortStyle.fixedWidth = 16f;
        disconnectedPortStyle.fixedHeight = 16f;

        // Resize handle style
        resizeStyle = new GUIStyle();
        resizeStyle.normal.background = CreateColorTexture(Color.gray);
        resizeStyle.fixedWidth = 16f;
        resizeStyle.fixedHeight = 16f;

        stylesInitialized = true;
    }

    private Texture2D CreateColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void OnGUI()
    {

        // Initialize styles on first OnGUI call
        InitializeStyles();

        lastMousePosition = Event.current.mousePosition;

        DrawToolbar();
        
        if (currentTree == null)
        {
            EditorGUILayout.HelpBox("No Node Tree selected! Drag a node tree asset into the field above.", MessageType.Info);
            return;
        }

        // Clear port positions at start of frame
        achievementPortPositions.Clear();

        // Create default nodes if tree is empty
        if (nodes == null || nodes.Count == 0)
        {
            CreateDefaultNodes();
        }

        Rect zoomArea = new Rect(0, 20, position.width, position.height - 20);
        
        HandleEvents(Event.current, zoomArea);

        DrawGrid(zoomArea, 20, 0.2f, Color.gray);
        DrawGrid(zoomArea, 100, 0.4f, Color.gray);

        DrawConnections();

        DrawNodes(zoomArea);

        DrawConnectionLine(Event.current, zoomArea);

        DrawZoomInfo();

        if (GUI.changed) Repaint();
    
    }
    private void CreateAchievementDefaultNodes()
    {
        AchievementTree achievementTree = currentTree as AchievementTree;
        if (achievementTree == null) return;

        // Create a default achievement node
        AchievementNode achievementNode = new AchievementNode
        {
            NodeID = "FirstAchievement",
            TitleText = "First Steps",
            DescriptionText = "Complete your first achievement!",
            graphPosition = new Rect(100, 200, 400, 400)
        };

        // Use the safe add method
        achievementTree.AddNode(achievementNode);
        achievementTree.StartNodeID = achievementNode.NodeID;

        EditorUtility.SetDirty(currentTree);
        AssetDatabase.SaveAssets(); // Save immediately
    }

    private void DeleteNode(INode nodeToDelete)
    {
        if (nodes.Count <= 1) return;

        // Remove all references to this node
        foreach (var node in nodes)
        {
            List<string> connectedIDs = node.GetConnectedNodeIDs();
            connectedIDs.RemoveAll(id => id == nodeToDelete.NodeID);
            
            if (node is AchievementNode achievementNode)
            {
                if (achievementNode.NextAchievements != null)
                    achievementNode.NextAchievements.RemoveAll(id => id == nodeToDelete.NodeID);
            }
        }
        
        // Update start node if needed
        if (nodeTreeInterface.StartNodeID == nodeToDelete.NodeID)
        {
            nodeTreeInterface.StartNodeID = nodes[0].NodeID;
        }
        
        // Use safe removal
        if (currentTree is NodeTree tree)
        {
            // We need to access the underlying list - this is a bit hacky but necessary
            // due to the interface abstraction
            var field = typeof(NodeTree).GetField("_nodes", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                var nodeList = field.GetValue(currentTree) as List<INode>;
                nodeList?.Remove(nodeToDelete);
            }
        }

        if (selectedNode == nodeToDelete)
        {
            selectedNode = null;
        }

        EditorUtility.SetDirty(currentTree);
        AssetDatabase.SaveAssets(); // Save changes
        GUI.changed = true;
    }
    private void CreateDefaultNodes()
    {
        if (nodeTreeInterface == null) return;

        // Create a default node based on the tree type
        // if (currentTree is DialogueINodeTree)
        // {
        //     CreateDialogueDefaultNodes();
        // }
        if (currentTree is AchievementTree)
        {
            CreateAchievementDefaultNodes();
        }
        else
        {
            // Generic fallback
            CreateGenericDefaultNode();
        }
    }

    // private void CreateDialogueDefaultNodes()
    // {
    //     DialogueINodeTree dialogueTree = (DialogueINodeTree) currentTree;
    //     if (dialogueTree == null) return;

    //     // Create StartNode
    //     DialogueINode startNode = new DialogueINode
    //     {
    //         nodeID = "StartNode",
    //         dialogueText = "Welcome! This is the start of your dialogue.",
    //         graphPosition = new Rect(100, 200, 350, 200)
    //     };

    //     // Create END node
    //     DialogueINode endNode = new DialogueINode
    //     {
    //         nodeID = "END", 
    //         dialogueText = "This conversation has ended.",
    //         graphPosition = new Rect(500, 200, 350, 200)
    //     };

    //     dialogueTree.nodes.Add(startNode);
    //     dialogueTree.nodes.Add(endNode);
    //     dialogueTree.startNodeID = startNode.nodeID;

    //     EditorUtility.SetDirty(currentTree);
    // }



    private void CreateGenericDefaultNode()
    {
        // Create a simple default node
        var nodeType = nodeTreeInterface.Nodes.GetType().GetGenericArguments()[0];
        INode defaultNode = Activator.CreateInstance(nodeType) as INode;

        if (defaultNode != null)
        {
            defaultNode.NodeID = "StartNode";
            defaultNode.GraphPosition = new Rect(100, 200, 350, 200);

            // Use reflection to add to nodes list
            var nodesProperty = currentTree.GetType().GetField("nodes");
            if (nodesProperty != null)
            {
                var nodesList = nodesProperty.GetValue(currentTree) as System.Collections.IList;
                nodesList?.Add(defaultNode);
            }

            nodeTreeInterface.StartNodeID = defaultNode.NodeID;
            EditorUtility.SetDirty(currentTree);
        }
    }
    private ConnectionInfo hoveredConnection = null;
    private void HandleConnectionHover(Event e, Rect zoomArea)
    {
        if (e.type == EventType.MouseMove)
        {
            ConnectionInfo newHovered = FindConnectionAtPosition(e.mousePosition);
            if (hoveredConnection != newHovered)
            {
                hoveredConnection = newHovered;
                GUI.changed = true;
            }
        }
    }
    private void HandleEvents(Event e, Rect zoomArea)
    {
        HandleConnectionRightClick(e, zoomArea);
        HandleConnectionHover(e, zoomArea); 
        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0) // Left click
                {
                    foreach (var kvp in achievementPortPositions)
                    {
                        Vector2 portPos = kvp.Value;
                        Rect portRect = new Rect(portPos.x - 8, portPos.y - 8, 16, 16);
                        if (portRect.Contains(lastMousePosition))
                        {
                            connectingFrom = kvp.Key;
                            selectedNode = kvp.Key;
                            e.Use();
                            return;
                        }
                    }
                    resizingNode = null;
                    draggingNode = null;
                    
                    if (nodes != null)
                    {
                        // Check for achievement port clicks
                        foreach (var node in nodes)
                        {
                            if (node is AchievementNode && achievementPortPositions.ContainsKey(node))
                            {
                                Vector2 portPos = achievementPortPositions[node];
                                Rect portRect = new Rect(portPos.x - 8, portPos.y - 8, 16, 16);
                                if (portRect.Contains(lastMousePosition))
                                {
                                    connectingFrom = node;
                                    selectedNode = node;
                                    e.Use();
                                    return;
                                }
                            }
                        }
                        
                        // Then check for resize handle (your existing code)
                        foreach (var node in nodes)
                        {
                            Rect screenRect = NodeToScreenRect(node);
                            screenRect.x += zoomArea.x;
                            screenRect.y += zoomArea.y;
                            Rect resizeHandle = new Rect(
                                screenRect.x + screenRect.width - 16,
                                screenRect.y + screenRect.height - 16,
                                16, 16
                            );
                            if (resizeHandle.Contains(lastMousePosition))
                            {
                                resizingNode = node;
                                resizeStart = ScreenToWorldPosition(lastMousePosition);
                                resizeStartRect = node.GraphPosition;
                                selectedNode = node;
                                e.Use();
                                return;
                            }
                        }
                        
                        // Then check for node dragging (ONLY on title bar)
                        foreach (var node in nodes)
                        {
                            Rect screenRect = NodeToScreenRect(node);
                            Rect titleBarRect = new Rect(
                                screenRect.x,
                                screenRect.y,
                                screenRect.width-50,
                                TITLE_BAR_HEIGHT * zoomLevel * 1.5f
                            );
                            
                            if (titleBarRect.Contains(lastMousePosition))
                            {
                                draggingNode = node;
                                selectedNode = node;
                                dragOffset = ScreenToWorldPosition(lastMousePosition) - node.GraphPosition.position;
                                e.Use();
                                return;
                            }
                        }
                    }
                    
                    // If clicked on empty space, deselect everything
                    selectedNode = null;
                    connectingFrom = null;
                    GUI.changed = true;
                }
                else if (e.button == 1 || e.button == 2) // Right click or Middle mouse button
                {
                    isPanning = true;
                }
                break;
                
            case EventType.MouseUp:
                if (e.button == 0)
                {
                    if (connectingFrom != null)
                    {
                        // Check if clicked on a node to connect
                        for (int i = 0; i < nodes.Count; i++)
                        {
                            Rect screenRect = NodeToScreenRect(nodes[i]);
                            if (screenRect.Contains(lastMousePosition) && nodes[i] != connectingFrom)
                            {
                                HandleNodeConnection(connectingFrom, nodes[i]);
                                break;
                            }
                        }
                        connectingFrom = null;
                        e.Use();
                    }
                    resizingNode = null;
                    draggingNode = null;
                }
                else if (e.button == 1 || e.button == 2)
                {
                    isPanning = false;
                }
                break;
                
            case EventType.MouseDrag:
                if (resizingNode != null)
                {
                    // Handle node resizing
                    Vector2 worldMousePos = ScreenToWorldPosition(lastMousePosition);
                    Vector2 delta = worldMousePos - resizeStart;
                    resizingNode.GraphPosition = new Rect(
                        resizeStartRect.x,
                        resizeStartRect.y,
                        Mathf.Max(350, resizeStartRect.width + delta.x),
                        Mathf.Max(200, resizeStartRect.height + delta.y)
                    );
                    GUI.changed = true;
                    e.Use();
                }
                else if (draggingNode != null)
                {
                    // Handle node dragging
                    Vector2 worldMousePos = ScreenToWorldPosition(lastMousePosition);
                    draggingNode.GraphPosition = new Rect(
                        worldMousePos - dragOffset,
                        draggingNode.GraphPosition.size
                    );
                    GUI.changed = true;
                    e.Use();
                }
                else if (isPanning && (e.button == 1 || e.button == 2))
                {
                    // Handle panning with right click or middle mouse
                    zoomPan += e.delta;
                    GUI.changed = true;
                    e.Use();
                }
                break;
                
            case EventType.ScrollWheel:
                // Zoom with scroll wheel centered on mouse position
                float oldZoom = zoomLevel;
                float zoomChange = -e.delta.y * 0.01f;
                zoomLevel = Mathf.Clamp(zoomLevel + zoomChange, MIN_ZOOM, MAX_ZOOM);
                
                // Adjust zoom origin to zoom towards mouse position
                Vector2 mouseWorldPosBeforeZoom = ScreenToWorldPosition(lastMousePosition, oldZoom);
                zoomPan = lastMousePosition - (mouseWorldPosBeforeZoom * zoomLevel);
                
                GUI.changed = true;
                e.Use();
                break;
                
            case EventType.KeyDown:
                if (e.keyCode == KeyCode.Delete && selectedNode != null)
                {
                    DeleteNode(selectedNode);
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Escape)
                {
                    connectingFrom = null;
                    selectedNode = null;
                    resizingNode = null;
                    draggingNode = null;
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Equals)
                {
                    ZoomAtPosition(lastMousePosition, 0.1f);
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Minus) 
                {
                    ZoomAtPosition(lastMousePosition, -0.1f);
                    e.Use();
                }
                break;
        }
    }

    private void HandleNodeConnection(INode fromNode, INode toNode)
    {
        // Handle connections based on node type
        // if (fromNode is DialogueINode DialogueINode)
        // {
        //     // For dialogue nodes, add a new choice
        //     var newChoice = new DialogueChoice();
        //     newChoice.targetNodeID = toNode.NodeID;
        //     newChoice.choiceText = $"Go to {toNode.NodeID}";
        //     DialogueINode.choices.Add(newChoice);
        // }
        if (fromNode is AchievementNode achievementNode)
        {
            // For achievement nodes, add to NextAchievements
            if (achievementNode.NextAchievements == null)
                achievementNode.NextAchievements = new List<string>();
            
            if (!achievementNode.NextAchievements.Contains(toNode.NodeID))
                achievementNode.NextAchievements.Add(toNode.NodeID);
        }
        
        EditorUtility.SetDirty(currentTree);
    }

    private Vector2 ScreenToWorldPosition(Vector2 screenPos, float customZoom = -1)
    {
        float zoom = customZoom > 0 ? customZoom : zoomLevel;
        return (screenPos - zoomPan) / zoom;
    }

    private Vector2 WorldToScreenPosition(Vector2 worldPos)
    {
        return (worldPos * zoomLevel) + zoomPan;
    }

    private Rect NodeToScreenRect(INode node)
    {
        Vector2 screenPos = WorldToScreenPosition(node.GraphPosition.position);
        Vector2 screenSize = node.GraphPosition.size * zoomLevel;
        return new Rect(screenPos, screenSize);
    }

    private void ZoomAtPosition(Vector2 screenCenter, float zoomChange)
    {
        float oldZoom = zoomLevel;
        zoomLevel = Mathf.Clamp(zoomLevel + zoomChange, MIN_ZOOM, MAX_ZOOM);
        
        // Adjust zoom origin to zoom towards position
        Vector2 mouseWorldPosBeforeZoom = ScreenToWorldPosition(screenCenter, oldZoom);
        zoomPan = screenCenter - (mouseWorldPosBeforeZoom * zoomLevel);
    }

    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        EditorGUI.BeginChangeCheck();
        currentTree = EditorGUILayout.ObjectField(currentTree, typeof(NodeTree), false) as NodeTree;
        if (currentTree != null && !(currentTree is INodeTree))
        {
            Debug.LogWarning($"Selected object {currentTree.name} does not implement INodeTree. Please select a valid node tree.");
            currentTree = null;
        }
        
        if (EditorGUI.EndChangeCheck())
        {
            selectedNode = null;
            connectingFrom = null;
            nodeTreeInterface = currentTree as INodeTree;
        }
        
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("New Node", EditorStyles.toolbarButton))
        {
            CreateNode();
        }
        
        if (GUILayout.Button("Center View", EditorStyles.toolbarButton))
        {
            zoomLevel = 1.0f;
            zoomPan = Vector2.zero;
        }
        
        if (GUILayout.Button("Clear All", EditorStyles.toolbarButton) && currentTree != null)
        {
            if (EditorUtility.DisplayDialog("Clear All Nodes", "Are you sure you want to delete all nodes?", "Yes", "No"))
            {
                ClearAllNodes();
            }
        }
        
        if (GUILayout.Button("Save", EditorStyles.toolbarButton) && currentTree != null)
        {
            EditorUtility.SetDirty(currentTree);
            AssetDatabase.SaveAssets();
            Debug.Log("Node Tree saved!");
        }
        
        GUILayout.EndHorizontal();
    }

    private void ClearAllNodes()
    {
        if (nodeTreeInterface == null) return;
        
        nodes.Clear();
        nodeTreeInterface.StartNodeID = "";
        EditorUtility.SetDirty(currentTree);
    }

    private void DrawGrid(Rect area, float gridSpacing, float gridOpacity, Color gridColor)
    {
        // Use fixed grid spacing regardless of zoom
        float worldGridSpacing = gridSpacing;
        
        // Calculate the visible world area
        Vector2 worldTopLeft = ScreenToWorldPosition(area.position);
        Vector2 worldBottomRight = ScreenToWorldPosition(area.position + area.size);
        
        // Calculate the starting grid lines
        float startX = Mathf.Floor(worldTopLeft.x / worldGridSpacing) * worldGridSpacing;
        float startY = Mathf.Floor(worldTopLeft.y / worldGridSpacing) * worldGridSpacing;
        
        Handles.BeginGUI();
        Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

        // Draw vertical lines
        for (float x = startX; x <= worldBottomRight.x; x += worldGridSpacing)
        {
            Vector2 screenStart = WorldToScreenPosition(new Vector2(x, worldTopLeft.y));
            Vector2 screenEnd = WorldToScreenPosition(new Vector2(x, worldBottomRight.y));
            
            if (screenStart.x >= area.x && screenStart.x <= area.x + area.width)
            {
                Handles.DrawLine(
                    new Vector3(screenStart.x, area.y, 0),
                    new Vector3(screenEnd.x, area.y + area.height, 0)
                );
            }
        }

        // Draw horizontal lines
        for (float y = startY; y <= worldBottomRight.y; y += worldGridSpacing)
        {
            Vector2 screenStart = WorldToScreenPosition(new Vector2(worldTopLeft.x, y));
            Vector2 screenEnd = WorldToScreenPosition(new Vector2(worldBottomRight.x, y));
            
            if (screenStart.y >= area.y && screenStart.y <= area.y + area.height)
            {
                Handles.DrawLine(
                    new Vector3(area.x, screenStart.y, 0),
                    new Vector3(area.x + area.width, screenEnd.y, 0)
                );
            }
        }

        Handles.color = Color.white;
        Handles.EndGUI();
    }

    private void DrawNodes(Rect zoomArea)
    {
        if (nodes == null) return;

        for (int i = 0; i < nodes.Count; i++)
        {
            INode node = nodes[i];
            
            // Get node drawer for this type
            INodeDrawer drawer = GetNodeDrawer(node.GetType());
            if (drawer != null)
            {
                Vector2 requiredSize = drawer.CalculateRequiredSize(node);
                node.GraphPosition = new Rect(
                    node.GraphPosition.position,
                    new Vector2(Mathf.Max(350, requiredSize.x), Mathf.Max(200, requiredSize.y))
                );
            }

            Rect screenRect = NodeToScreenRect(node);
            screenRect.x += zoomArea.x;
            screenRect.y += zoomArea.y;

            bool isSelected = selectedNode == node;
            GUIStyle style = isSelected ? selectedNodeStyle : nodeStyle;
            
            GUI.Box(screenRect, "", style);
            
            DrawNodeContent(node, i, screenRect);
            
            // Draw resize handle
            Rect resizeHandle = new Rect(
                screenRect.x + screenRect.width - 16,  
                screenRect.y + screenRect.height - 16, 
                16, 16
            );
            GUI.Box(resizeHandle, "", resizeStyle);
        }
    }
    private string GetNodeDisplayName(INode node)
    {
        if (node is AchievementNode achievementNode)
        {
            return string.IsNullOrEmpty(achievementNode.TitleText) ? 
                $"Unnamed Achievement ({achievementNode.NodeID})" : 
                achievementNode.TitleText;
        }
        
        return $"{node.GetType().Name} ({node.NodeID})";
    }
   private void DrawNodeContent(INode node, int nodeIndex, Rect screenRect)
    {
        Matrix4x4 originalMatrix = GUI.matrix;
        
        try
        {
            Matrix4x4 scaleMatrix = Matrix4x4.TRS(
                new Vector3(screenRect.x, screenRect.y, 0),
                Quaternion.identity,
                new Vector3(zoomLevel, zoomLevel, 1f)
            );
            GUI.matrix = scaleMatrix * originalMatrix;
            
            Rect worldContentRect = new Rect(0, 0, node.GraphPosition.width, node.GraphPosition.height);
            
            GUILayout.BeginArea(worldContentRect);
            
            // Node title header
            string headerText = GetNodeDisplayName(node);
            Rect headerRect = GUILayoutUtility.GetRect(screenRect.width, TITLE_BAR_HEIGHT, GUILayout.ExpandWidth(true));
            GUI.Box(headerRect, headerText, nodeHeaderStyle);

            // Node delete button
            Rect deleteButtonRect = new Rect(worldContentRect.width - 25, headerRect.height/2-10, 20, 20);
            Color originalColor = GUI.color;
            GUI.color = Color.red; 
            if (GUI.Button(deleteButtonRect, "X"))
            {
                if (EditorUtility.DisplayDialog("Delete Node", "Are you sure you want to delete this node?", "Yes", "No"))
                {
                    DeleteNode(node);
                    return;
                }
            }
            GUI.color = originalColor;

            // Set as start button
            if (GUILayout.Button("Set as Start Node"))
            {
                nodeTreeInterface.StartNodeID = node.NodeID;
                EditorUtility.SetDirty(currentTree);
            }

            // Use the appropriate node drawer
            INodeDrawer drawer = GetNodeDrawer(node.GetType());
            if (drawer != null)
            {
                drawer.DrawNode(node, nodeTreeInterface, worldContentRect.width, () => {
                    DeleteNode(node);
                });
                
                // Track port position for achievement nodes
                if (node is AchievementNode && drawer is AchievementNodeDrawer achievementDrawer)
                {
                    // Calculate the exact port position in screen coordinates
                    Rect portWorldRect = achievementDrawer.LastOutputPortRect;
                    Vector2 portWorldPos = new Vector2(
                        worldContentRect.x + portWorldRect.x + portWorldRect.width / 2,
                        worldContentRect.y + portWorldRect.y + portWorldRect.height / 2
                    );
                    
                    // Convert to screen coordinates
                    Vector2 portScreenPos = new Vector2(
                        screenRect.x + portWorldPos.x * zoomLevel,
                        screenRect.y + portWorldPos.y * zoomLevel
                    );
                    
                    achievementPortPositions[node] = portScreenPos;
                    achievementDrawerPortRects[achievementDrawer] = portWorldRect;
                }
            }
            else
            {
                EditorGUILayout.LabelField("No drawer available for this node type");
            }
            
            GUILayout.EndArea();
        }
        finally
        {
            GUI.matrix = originalMatrix;
        }
    }

    private INodeDrawer GetNodeDrawer(System.Type nodeType)
    {
        if (nodeDrawers.ContainsKey(nodeType))
            return nodeDrawers[nodeType];
        return null;
    }

    private void DrawConnections()
    {
        if (nodes == null) return;

        Handles.BeginGUI();
        
        foreach (var node in nodes)
        {
            // Draw achievement connections
            if (node is AchievementNode achievementNode)
            {
                if (achievementNode.NextAchievements != null)
                {
                    foreach (string targetNodeID in achievementNode.NextAchievements)
                    {
                        INode targetNode = nodes.Find(n => n.NodeID == targetNodeID);
                        if (targetNode != null)
                        {
                            // Start from the port position (red box)
                            Vector2 startPos = achievementPortPositions.ContainsKey(node) ? 
                                achievementPortPositions[node] : 
                                GetRightInputPosition(node);
                            
                            Vector2 endPos = GetDefaultOutputPosition(targetNode);
                            
                            // Calculate tangents for a smooth curve
                            Vector2 startTangent = startPos + Vector2.right * 50;
                            Vector2 endTangent = endPos + Vector2.left * 50;
                            
                            bool isHovered = hoveredConnection != null && 
                                            hoveredConnection.FromNode == node && 
                                            hoveredConnection.ToNode == targetNode;
                            
                            Color connectionColor = isHovered ? Color.red : Color.yellow;
                            float thickness = isHovered ? 5f : 3f;

                            // Draw the bezier curve
                            Handles.DrawBezier(startPos, endPos, startTangent, endTangent, Color.yellow, null, 3f);
                            
                            // Draw small circles at connection points
                            Handles.color = Color.yellow;
                            Handles.DrawSolidDisc(startPos, Vector3.forward, 4f);
                            Handles.DrawSolidDisc(endPos, Vector3.forward, 4f);
                            Handles.color = Color.white;
                        }
                    }
                }
            }
            
            // Keep existing dialogue connections
            List<string> connectedNodeIDs = node.GetConnectedNodeIDs();
            foreach (string targetNodeID in connectedNodeIDs)
            {
                INode targetNode = nodes.Find(n => n.NodeID == targetNodeID);
                if (targetNode != null && !(node is AchievementNode))
                {
                    // Your existing dialogue connection drawing
                    Vector2 startPos = GetRightInputPosition(node);
                    Vector2 endPos = GetDefaultOutputPosition(targetNode);
                    
                    Vector2 startTangent = startPos + Vector2.right * 50;
                    Vector2 endTangent = endPos + Vector2.left * 50;
                    
                    Handles.DrawBezier(startPos, endPos, startTangent, endTangent, Color.green, null, 3f);
                    
                    Handles.color = Color.green;
                    Handles.DrawSolidDisc(startPos, Vector3.forward, 4f);
                    Handles.DrawSolidDisc(endPos, Vector3.forward, 4f);
                    Handles.color = Color.white;
                }
            }
        }
        
        Handles.EndGUI();
    }

    private Vector2 GetRightInputPosition(INode node)
    {
        Rect screenRect = NodeToScreenRect(node);
        return new Vector2(
            screenRect.x + screenRect.width,
            screenRect.y + screenRect.height / 2
        );
    }

    private Vector2 GetDefaultOutputPosition(INode node)
    {
        Rect screenRect = NodeToScreenRect(node);
        return new Vector2(
            screenRect.x,
            screenRect.y + screenRect.height / 2
        );
    }
    
    private void HandleConnectionRightClick(Event e, Rect zoomArea)
    {
        if (e.type == EventType.MouseDown && e.button == 1) // Right click
        {
            // Check if right-clicked on a connection line
            ConnectionInfo clickedConnection = FindConnectionAtPosition(e.mousePosition);
            if (clickedConnection != null)
            {
                ShowConnectionContextMenu(clickedConnection);
                e.Use();
            }
        }
    }

    private ConnectionInfo FindConnectionAtPosition(Vector2 screenPosition)
    {
        if (nodes == null) return null;

        foreach (var node in nodes)
        {
            if (node is AchievementNode achievementNode)
            {
                if (achievementNode.NextAchievements != null)
                {
                    foreach (string targetNodeID in achievementNode.NextAchievements)
                    {
                        INode targetNode = nodes.Find(n => n.NodeID == targetNodeID);
                        if (targetNode != null)
                        {
                            // Calculate connection points
                            Vector2 startPos = achievementPortPositions.ContainsKey(node) ? 
                                achievementPortPositions[node] : 
                                GetDefaultOutputPosition(node);
                            
                            Vector2 endPos = GetRightInputPosition(targetNode);
                            
                            // Check if click is near the connection line
                            if (IsPointNearConnection(screenPosition, startPos, endPos))
                            {
                                return new ConnectionInfo(node, targetNode);
                            }
                        }
                    }
                }
            }
        }
        
        return null;
    }

    private bool IsPointNearConnection(Vector2 point, Vector2 start, Vector2 end, float maxDistance = 10f)
    {
        // Simple distance to line segment check
        Vector2 line = end - start;
        float lineLength = line.magnitude;
        Vector2 lineDir = line.normalized;
        
        Vector2 pointToStart = point - start;
        float dot = Vector2.Dot(pointToStart, lineDir);
        
        if (dot < 0) dot = 0;
        if (dot > lineLength) dot = lineLength;
        
        Vector2 closestPoint = start + lineDir * dot;
        float distance = Vector2.Distance(point, closestPoint);
        
        return distance <= maxDistance;
    }

    private void ShowConnectionContextMenu(ConnectionInfo connection)
    {
        string fromTitle = GetNodeDisplayName(connection.FromNode);
        string toTitle = GetNodeDisplayName(connection.ToNode);
        
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent($"Remove Connection: {fromTitle} → {toTitle}"), false, () => RemoveConnection(connection));
        menu.ShowAsContext();
    }

    private void RemoveConnection(ConnectionInfo connection)
    {
        if (connection.FromNode is AchievementNode achievementNode)
        {
            achievementNode.RemoveConnection(connection.ToNode.NodeID);
            EditorUtility.SetDirty(currentTree);
            GUI.changed = true;
        }
    }

    // Helper class to store connection information
    private class ConnectionInfo
    {
        public INode FromNode { get; }
        public INode ToNode { get; }
        
        public ConnectionInfo(INode fromNode, INode toNode)
        {
            FromNode = fromNode;
            ToNode = toNode;
        }
    }
     private void DrawConnectionLine(Event e, Rect zoomArea)
    {
        if (connectingFrom != null)
        {
            Vector2 startPos;
            
            if (connectingFrom is AchievementNode && achievementPortPositions.ContainsKey(connectingFrom))
            {
                // Start from the actual port position (red box)
                startPos = achievementPortPositions[connectingFrom];
            }
            else
            {
                // Fallback to right side
                startPos = GetDefaultOutputPosition(connectingFrom);
            }
            
            Vector2 endPos = new Vector2(e.mousePosition.x, e.mousePosition.y);
            
            // Draw the connection line with proper curve
            Handles.BeginGUI();
            
            // Calculate tangents for smooth curve
            Vector2 startTangent = startPos + Vector2.right * 50;
            Vector2 endTangent = endPos;
            if (endPos.x > startPos.x)
            {
                endTangent = endPos + Vector2.left * 50;
            }
            else
            {
                endTangent = endPos + Vector2.right * 50;
            }
            
            Handles.DrawBezier(
                startPos,
                endPos,
                startTangent,
                endTangent,
                Color.yellow,
                null,
                2f
            );
            
            // Draw port circle
            Handles.color = Color.yellow;
            Handles.DrawSolidDisc(startPos, Vector3.forward, 4f);
            Handles.color = Color.white;
            
            Handles.EndGUI();
            
            GUI.changed = true;
        }
    }
    
    private void DrawZoomInfo()
    {
        // Draw zoom level in corner
        GUILayout.BeginArea(new Rect(position.width - 120, position.height - 30, 110, 25));
        GUILayout.BeginHorizontal("box");
        GUILayout.Label($"Zoom: {zoomLevel:F1}x");
        if (GUILayout.Button("Reset", GUILayout.Width(50)))
        {
            zoomLevel = 1.0f;
            zoomPan = Vector2.zero;
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void CreateNode()
    {
        if (nodeTreeInterface == null) return;

        // Create node based on tree type
        // if (currentTree is DialogueINodeTree dialogueTree)
        // {
        //     CreateDialogueNode(dialogueTree);
        // }
        if (currentTree is AchievementTree achievementTree)
        {
            CreateAchievementNode(achievementTree);
        }
        else
        {
            CreateGenericNode();
        }
    }

    // private void CreateDialogueNode(DialogueINodeTree dialogueTree)
    // {
    //     DialogueINode newNode = new DialogueINode
    //     {
    //         nodeID = $"node_{System.Guid.NewGuid().ToString().Substring(0, 8)}",
    //         dialogueText = "Enter dialogue text here...",
    //         graphPosition = new Rect(100 + nodes.Count * 30, 100 + nodes.Count * 30, 350, 200)
    //     };

    //     dialogueTree.nodes.Add(newNode);
        
    //     if (string.IsNullOrEmpty(nodeTreeInterface.StartNodeID))
    //     {
    //         nodeTreeInterface.StartNodeID = newNode.nodeID;
    //     }

    //     EditorUtility.SetDirty(currentTree);
    //     GUI.changed = true;
    // }

    private void CreateAchievementNode(AchievementTree achievementTree)
    {
        AchievementNode newNode = new AchievementNode
        {
            NodeID = $"achievement_{System.Guid.NewGuid().ToString().Substring(0, 8)}",
            TitleText = "New Achievement",
            DescriptionText = "Achievement description...",
            graphPosition = new Rect(100 + nodes.Count * 30, 100 + nodes.Count * 30, 400, 400)
        };

        achievementTree.AddNode(newNode);
        
        if (string.IsNullOrEmpty(nodeTreeInterface.StartNodeID))
        {
            nodeTreeInterface.StartNodeID = newNode.NodeID;
        }

        EditorUtility.SetDirty(currentTree);
        GUI.changed = true;
    }

    private void CreateGenericNode()
    {
        // Create a generic node using reflection
        var nodeType = nodeTreeInterface.Nodes.GetType().GetGenericArguments()[0];
        INode newNode = Activator.CreateInstance(nodeType) as INode;
        
        if (newNode != null)
        {
            newNode.NodeID = $"node_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            newNode.GraphPosition = new Rect(100 + nodes.Count * 30, 100 + nodes.Count * 30, 350, 200);
            
            // Use reflection to add to nodes list
            var nodesProperty = currentTree.GetType().GetField("nodes");
            if (nodesProperty != null)
            {
                var nodesList = nodesProperty.GetValue(currentTree) as System.Collections.IList;
                nodesList?.Add(newNode);
            }
            
            if (string.IsNullOrEmpty(nodeTreeInterface.StartNodeID))
            {
                nodeTreeInterface.StartNodeID = newNode.NodeID;
            }

            EditorUtility.SetDirty(currentTree);
            GUI.changed = true;
        }
    }
}