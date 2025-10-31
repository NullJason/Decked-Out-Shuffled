using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DialogueNodeEditor : EditorWindow
{
    private DialogueTree currentTree;
    private List<DialogueNode> nodes => currentTree?.nodes;
    
    private GUIStyle nodeStyle;
    private GUIStyle selectedNodeStyle;
    private GUIStyle connectedPortStyle;
    private GUIStyle disconnectedPortStyle;
    private GUIStyle nodeHeaderStyle;
    private GUIStyle resizeStyle;
    
    private DialogueNode selectedNode;
    private DialogueNode connectingFrom;
    private int connectingChoiceIndex = -1;
    private Vector2 panOffset;
    private bool isPanning;
    private Vector2 lastMousePosition;
    
    // Zoom functionality
    private float zoomLevel = 1.0f;
    private const float MIN_ZOOM = 0.3f;
    private const float MAX_ZOOM = 2.0f;
    private Vector2 zoomPan = Vector2.zero;
    
    // Node resizing
    private DialogueNode resizingNode;
    private Vector2 resizeStart;
    private Rect resizeStartRect;
    
    // Node dragging
    private DialogueNode draggingNode;
    private Vector2 dragOffset;
    
    // Port positions for connection lines
    private Dictionary<DialogueChoice, Vector2> choicePortPositions = new Dictionary<DialogueChoice, Vector2>();
    
    // Style initialization flag
    private bool stylesInitialized = false;
    
    // Title bar height for dragging
    private const float TITLE_BAR_HEIGHT = 25f;
    [MenuItem("Window/Dialogue Node Editor")]
    public static void OpenWindow()
    {
        GetWindow<DialogueNodeEditor>("Dialogue Editor");
    }

    private void OnEnable()
    {
        Debug.Log("Resize Handle currently doesn't work!");
        Debug.Log("TODO: dialogue text box field should auto display downwards when text out of box.");
        // note, don't init styles here, must wait ongui 
    }

    private void InitializeStyles()
    {
        if (stylesInitialized) return;

        // Node styles
        nodeStyle = new GUIStyle();
        nodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
        if (nodeStyle.normal.background == null)
            nodeStyle.normal.background = CreateColorTexture(new Color(0.3f, 0.3f, 0.3f, 0.9f));
        nodeStyle.border = new RectOffset(12, 12, 12, 12);
        nodeStyle.padding = new RectOffset(10, 10, 10, 10);
        
        selectedNodeStyle = new GUIStyle();
        selectedNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1 on.png") as Texture2D;
        if (selectedNodeStyle.normal.background == null)
            selectedNodeStyle.normal.background = CreateColorTexture(new Color(0.4f, 0.4f, 0.6f, 0.9f));
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
            EditorGUILayout.HelpBox("No Dialogue Tree selected! Drag a DialogueTree asset into the field above.", MessageType.Info);
            return;
        }

        // Create default nodes if tree is empty
        if (currentTree.nodes.Count == 0)
        {
            CreateDefaultNodes();
        }

        choicePortPositions.Clear();

        Rect zoomArea = new Rect(0, 20, position.width, position.height - 20);
        
        HandleEvents(Event.current, zoomArea);

        DrawGrid(zoomArea, 20, 0.2f, Color.gray);
        DrawGrid(zoomArea, 100, 0.4f, Color.gray);

        DrawConnections();

        DrawNodes(zoomArea);

        DrawConnectionLine(Event.current, zoomArea);

        DrawZoomInfo();

        DrawDebugInfo();

        if (GUI.changed) Repaint();
    }

    private void CreateDefaultNodes()
    {
        if (currentTree == null) return;

        // Create StartNode
        DialogueNode startNode = new DialogueNode
        {
            nodeID = "StartNode",
            dialogueText = "Welcome! This is the start of your dialogue.",
            graphPosition = new Rect(100, 200, 350, 200)
        };

        // Create END node
        DialogueNode endNode = new DialogueNode
        {
            nodeID = "END", 
            dialogueText = "This conversation has ended.",
            graphPosition = new Rect(500, 200, 350, 200)
        };

        currentTree.nodes.Add(startNode);
        currentTree.nodes.Add(endNode);
        currentTree.startNodeID = startNode.nodeID;

        EditorUtility.SetDirty(currentTree);
        Debug.Log("Created default StartNode and END nodes");
    }

    private void HandleEvents(Event e, Rect zoomArea)
    {
        // lastMousePosition = e.mousePosition;
        
        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0) // Left click
                {
                    // First check for resize handle
                    resizingNode = null;
                    draggingNode = null;
                    
                    if (nodes != null)
                    {
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
                                resizeStartRect = node.graphPosition;
                                selectedNode = node;
                                e.Use();
                                return;
                            }
                        }
                    }
                    
                    // Then check for node dragging (ONLY on title bar)
                    if (nodes != null)
                    {
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
                                dragOffset = ScreenToWorldPosition(lastMousePosition) - node.graphPosition.position;
                                e.Use();
                                return;
                            }
                        }
                    }
                    
                    // If clicked on empty space, deselect everything
                    selectedNode = null;
                    connectingFrom = null;
                    connectingChoiceIndex = -1;
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
                                if (connectingChoiceIndex >= 0 && connectingChoiceIndex < connectingFrom.choices.Count)
                                {
                                    // Update existing choice
                                    connectingFrom.choices[connectingChoiceIndex].targetNodeID = nodes[i].nodeID;
                                }
                                else
                                {
                                    // Create new choice
                                    var newChoice = new DialogueChoice();
                                    newChoice.targetNodeID = nodes[i].nodeID;
                                    connectingFrom.choices.Add(newChoice);
                                }
                                EditorUtility.SetDirty(currentTree);
                                break;
                            }
                        }
                        connectingFrom = null;
                        connectingChoiceIndex = -1;
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
                    resizingNode.graphPosition = new Rect(
                        resizeStartRect.x,
                        resizeStartRect.y,
                        Mathf.Max(350, resizeStartRect.width + delta.x), // Increased minimum width
                        Mathf.Max(200, resizeStartRect.height + delta.y)
                    );
                    GUI.changed = true;
                    e.Use();
                }
                else if (draggingNode != null)
                {
                    // Handle node dragging
                    Vector2 worldMousePos = ScreenToWorldPosition(lastMousePosition);
                    draggingNode.graphPosition.position = worldMousePos - dragOffset;
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
                    connectingChoiceIndex = -1;
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
                else if (e.keyCode == KeyCode.Escape) 
                {
                    zoomLevel = 1.0f;
                    zoomPan = Vector2.zero;
                    e.Use();
                }
                break;
        }
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

    private Rect NodeToScreenRect(DialogueNode node)
    {
        Vector2 screenPos = WorldToScreenPosition(node.graphPosition.position);
        Vector2 screenSize = node.graphPosition.size * zoomLevel;
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
        currentTree = (DialogueTree)EditorGUILayout.ObjectField(currentTree, typeof(DialogueTree), false);
        if (EditorGUI.EndChangeCheck())
        {
            selectedNode = null;
            connectingFrom = null;
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
                currentTree.nodes.Clear();
                currentTree.startNodeID = "";
                EditorUtility.SetDirty(currentTree);
            }
        }
        
        if (GUILayout.Button("Save", EditorStyles.toolbarButton) && currentTree != null)
        {
            EditorUtility.SetDirty(currentTree);
            AssetDatabase.SaveAssets();
            Debug.Log("Dialogue Tree saved!");
        }
        
        GUILayout.EndHorizontal();
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
            DialogueNode node = nodes[i];
            
            // Auto-resize node based on content
            Vector2 requiredSize = CalculateRequiredNodeSize(node);
            node.graphPosition.width = Mathf.Max(350, requiredSize.x); // Increased minimum width
            node.graphPosition.height = Mathf.Max(200, requiredSize.y);

            // Apply zoom and pan to node position for display
            Rect screenRect = NodeToScreenRect(node);
            screenRect.x += zoomArea.x;
            screenRect.y += zoomArea.y;

            bool isSelected = selectedNode == node;

            //todo: builtin skins/darkskin/images/node1.png doesn't work well with rect sizes.
            GUIStyle style = isSelected ? selectedNodeStyle : nodeStyle;
            
            // Draw node background
            // GUI.Box(screenRect, "", style);
            GUI.Box(screenRect, "");
            
            DrawNodeContent(node, i, screenRect);
            
            // Draw resize handle
            Rect resizeHandle = new Rect(
                screenRect.x + screenRect.width - 16,  
                screenRect.y + screenRect.height - 16, 
                16, 16
            );
            GUI.Box(resizeHandle, "");
            
        }
    }

    private Vector2 CalculateRequiredNodeSize(DialogueNode node)
    {
        float width = 350f; // Base width
        float height = 160f; // Base height 
        
        // if (!string.IsNullOrEmpty(node.dialogueText))
        // {
        //     int lineCount = Mathf.Max(1, node.dialogueText.Length / 50); // Adjust chars per line based on width
        //     height += Mathf.Max(40, lineCount * 16);
        // }
        if (!string.IsNullOrEmpty(node.dialogueText))
        {
            float textAreaWidth = width - 20; 
            GUIContent textContent = new GUIContent(node.dialogueText);
            float textHeight = EditorStyles.textArea.CalcHeight(textContent, textAreaWidth);
            float minTextHeight = EditorGUIUtility.singleLineHeight * 3;
            height += Mathf.Max(minTextHeight, textHeight);
        }
        else
        {
            // Default height when no text
            height += EditorGUIUtility.singleLineHeight * 3;
        }
        height += node.choices.Count * 100f;
        
        height += 20f;
        
        return new Vector2(width, height);
    }

    private void DrawNodeContent(DialogueNode node, int nodeIndex, Rect screenRect)
    {
        Matrix4x4 originalMatrix = GUI.matrix;
        
        try
        {
            // Scale the content with the node
            Matrix4x4 scaleMatrix = Matrix4x4.TRS(
                new Vector3(screenRect.x, screenRect.y, 0),
                Quaternion.identity,
                new Vector3(zoomLevel, zoomLevel, 1f)
            );
            GUI.matrix = scaleMatrix * originalMatrix;
            
            // Calculate content area in world coordinates
            Rect worldContentRect = new Rect(0, 0, node.graphPosition.width, node.graphPosition.height);
            
            // Begin the content area
            GUILayout.BeginArea(worldContentRect);
            
            // Node title header - this is now the only draggable area
            Rect headerRect = GUILayoutUtility.GetRect(screenRect.width, TITLE_BAR_HEIGHT, GUILayout.ExpandWidth(true));
            GUI.Box(headerRect, $"Node {nodeIndex + 1}", nodeHeaderStyle);
            
            // Node delete
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
            GUI.color = originalColor; // Restore original color


            // Node ID field
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("ID:", GUILayout.Width(30));
            node.nodeID = EditorGUILayout.TextField(node.nodeID);
            GUILayout.EndHorizontal();

            // Set as start button
            if (GUILayout.Button("Set as Start Node"))
            {
                currentTree.startNodeID = node.nodeID;
                EditorUtility.SetDirty(currentTree);
            }

            // Character Headshot
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Headshot ID:", GUILayout.Width(80));
            node.characterHeadshotID = EditorGUILayout.IntField(node.characterHeadshotID);
            GUILayout.EndHorizontal();

            // Dialogue Text
            EditorGUILayout.LabelField("Dialogue Text:");
            
            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
            textAreaStyle.wordWrap = true; // This is the key line!

            float textAreaWidth = worldContentRect.width - 20;
            GUIContent textContent = new GUIContent(node.dialogueText);
            float textHeight = textAreaStyle.CalcHeight(textContent, textAreaWidth);

            float minTextHeight = EditorGUIUtility.singleLineHeight * 3;
            float finalTextHeight = Mathf.Max(minTextHeight, textHeight);

            node.dialogueText = EditorGUILayout.TextArea(node.dialogueText, textAreaStyle, 
                GUILayout.Height(finalTextHeight));
            // float availableWidth = worldContentRect.width - 20;
            // float textHeight = EditorStyles.textArea.CalcHeight(new GUIContent(node.dialogueText), availableWidth);
            // float minHeight = EditorStyles.textArea.lineHeight * 3;
            // node.dialogueText = EditorGUILayout.TextArea(node.dialogueText, GUILayout.Height(Mathf.Max(minHeight, textHeight)));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Choices:", EditorStyles.boldLabel);

            for (int i = 0; i < node.choices.Count; i++)
            {
                bool shouldContinue = DrawChoice(node, i, screenRect, worldContentRect);
                if (!shouldContinue) 
                {
                    break;
                }
            }

            if (GUILayout.Button("Add New Choice"))
            {
                node.choices.Add(new DialogueChoice());
            }
            
            GUILayout.EndArea();
        }
        finally
        {
            GUI.matrix = originalMatrix;
        }
    }

    private bool DrawChoice(DialogueNode node, int choiceIndex, Rect screenRect, Rect worldContentRect)
    {
        GUILayout.BeginVertical("box");
        
        // Choice header
        GUILayout.BeginHorizontal();
        
        // Choice content (left side)
        GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        
        // Choice text
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Choice {choiceIndex + 1}:", GUILayout.Width(60));
        node.choices[choiceIndex].choiceText = EditorGUILayout.TextField(node.choices[choiceIndex].choiceText);
        GUILayout.EndHorizontal();

        // Sort order
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Sort Order:", GUILayout.Width(60));
        node.choices[choiceIndex].sortOrder = EditorGUILayout.IntField(node.choices[choiceIndex].sortOrder);
        GUILayout.EndHorizontal();

        // Button Action MonoBehaviour
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Button Action:", GUILayout.Width(80));
        node.choices[choiceIndex].buttonAction = (MonoBehaviour)EditorGUILayout.ObjectField(
            node.choices[choiceIndex].buttonAction, 
            typeof(MonoBehaviour), 
            true, 
            GUILayout.ExpandWidth(true)
        );
        GUILayout.EndHorizontal();

        // Target node display
        if (!string.IsNullOrEmpty(node.choices[choiceIndex].targetNodeID))
        {
            EditorGUILayout.LabelField($"→ {node.choices[choiceIndex].targetNodeID}");
        }
        else
        {
            EditorGUILayout.LabelField("→ Not connected");
        }
        
        GUILayout.EndVertical(); // End choice content
        
        GUILayout.BeginVertical(GUILayout.Width(40), GUILayout.Height(60));
        
        GUILayout.FlexibleSpace();
        Rect outputPortRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16), GUILayout.Height(16));
        
        bool isConnected = !string.IsNullOrEmpty(node.choices[choiceIndex].targetNodeID);
        GUIStyle portStyle = isConnected ? connectedPortStyle : disconnectedPortStyle;
        
        if (portStyle != null)
        {
            GUI.Box(outputPortRect, "", portStyle);
        }
        else
        {
            GUI.Box(outputPortRect, "", GUI.skin.box);
        }
        
        Vector2 portWorldPos = new Vector2(
            worldContentRect.x + outputPortRect.x + outputPortRect.width / 2,
            worldContentRect.y + outputPortRect.y + outputPortRect.height / 2
        );
        Vector2 portScreenPos = new Vector2(
            screenRect.x + portWorldPos.x * zoomLevel,
            screenRect.y + portWorldPos.y * zoomLevel
        );
        choicePortPositions[node.choices[choiceIndex]] = portScreenPos;
        
        if (Event.current.type == EventType.MouseDown && outputPortRect.Contains(Event.current.mousePosition))
        {
            connectingFrom = node;
            connectingChoiceIndex = choiceIndex;
            Event.current.Use();
        }
        
        GUILayout.FlexibleSpace();
        
        bool choiceDeleted = false;
        if (GUILayout.Button("X", GUILayout.Width(20)))
        {
            node.choices.RemoveAt(choiceIndex);
            choiceDeleted = true;
        }
        
        GUILayout.EndVertical(); // End right-aligned controls
        
        GUILayout.EndHorizontal(); // End choice header
        GUILayout.EndVertical(); // End choice box
        
        // If choice was deleted, return false to break out of the loop
        return !choiceDeleted;
    }

    private void DrawConnections()
    {
        if (nodes == null) return;

        Handles.BeginGUI();
        
        foreach (var node in nodes)
        {
            for (int i = 0; i < node.choices.Count; i++)
            {
                var choice = node.choices[i];
                if (!string.IsNullOrEmpty(choice.targetNodeID))
                {
                    DialogueNode targetNode = nodes.Find(n => n.nodeID == choice.targetNodeID);
                    if (targetNode != null)
                    {
                        Vector2 startPos = Vector2.zero;
                        if (choicePortPositions.ContainsKey(choice))
                        {
                            startPos = choicePortPositions[choice];
                        }
                        else
                        {
                            Rect sourceScreenRect = NodeToScreenRect(node);
                            startPos = new Vector2(
                                sourceScreenRect.x + sourceScreenRect.width,
                                sourceScreenRect.y + 100 + (i * 40) // Approximate position
                            );
                        }
                        
                        Rect targetScreenRect = NodeToScreenRect(targetNode);
                        Vector2 endPos = new Vector2(
                            targetScreenRect.x,
                            targetScreenRect.y + targetScreenRect.height / 2
                        );
                        
                        
                        Vector2 startTangent = startPos + Vector2.right * 50;
                        Vector2 endTangent = endPos + Vector2.left * 50;
                        
                        Handles.DrawBezier(startPos, endPos, startTangent, endTangent, Color.green, null, 4f);
                        
                        Handles.color = Color.blue;
                        Handles.DrawSolidDisc(startPos, Vector3.forward, 4f);
                        Handles.DrawSolidDisc(endPos, Vector3.forward, 4f);
                        Handles.color = Color.white;
                    }
                }
            }
        }
        
        Handles.EndGUI();
    }
    private void DrawDebugInfo()
    {
        // mpos
        GUILayout.BeginArea(new Rect(10, position.height - 100, 500, 100));
        GUILayout.Label($"Mouse: {lastMousePosition}");
        GUILayout.Label($"Zoom: {zoomLevel}");
        GUILayout.Label($"ZoomPan: {zoomPan}");

        if (nodes != null && nodes.Count > 0 && selectedNode != null)
        {
            Rect screenRect = NodeToScreenRect(selectedNode);
            GUILayout.Label($"Node Screen: {screenRect}");
            GUILayout.Label($"Node World: {selectedNode.graphPosition}");
            Rect resizeHandle = new Rect(
                screenRect.x + screenRect.width - 16,
                screenRect.y + screenRect.height - 16,
                16, 16
            );
            GUILayout.Label($"Resize Handle: {resizeHandle}");
            GUILayout.Label($"Handle Contains Mouse: {resizeHandle.Contains(lastMousePosition)}");
        }
        GUILayout.EndArea();
    }
    private void DrawArrow(Vector3 position, Vector3 direction, Color color)
    {
        float arrowSize = 8f;
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 30, 0) * Vector3.back;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -30, 0) * Vector3.back;
        
        Handles.color = color;
        Handles.DrawLine(position, position + right * arrowSize);
        Handles.DrawLine(position, position + left * arrowSize);
        Handles.color = Color.white;
    }

    private void DrawConnectionLine(Event e, Rect zoomArea)
    {
        if (connectingFrom != null)
        {
            Vector2 startPos;
            
            if (connectingChoiceIndex >= 0 && connectingChoiceIndex < connectingFrom.choices.Count)
            {
                // Get the actual port position from our stored dictionary
                var choice = connectingFrom.choices[connectingChoiceIndex];
                if (choicePortPositions.ContainsKey(choice))
                {
                    startPos = choicePortPositions[choice];
                }
                else
                {
                    // Fallback calculation
                    float baseYOffset = 180f;
                    float choiceSpacing = 100f;
                    float choiceYOffset = baseYOffset + (connectingChoiceIndex * choiceSpacing);
                    
                    startPos = new Vector2(
                        (connectingFrom.graphPosition.x * zoomLevel) + zoomPan.x + zoomArea.x + (connectingFrom.graphPosition.width * zoomLevel) - 8,
                        (connectingFrom.graphPosition.y * zoomLevel) + zoomPan.y + zoomArea.y + Mathf.Min(choiceYOffset, connectingFrom.graphPosition.height * zoomLevel - 20)
                    );
                }
            }
            else
            {
                // Connecting from node center (new choice)
                startPos = new Vector2(
                    (connectingFrom.graphPosition.x * zoomLevel) + zoomPan.x + zoomArea.x + (connectingFrom.graphPosition.width * zoomLevel) / 2,
                    (connectingFrom.graphPosition.y * zoomLevel) + zoomPan.y + zoomArea.y + (connectingFrom.graphPosition.height * zoomLevel) / 2
                );
            }
            
            Vector2 endPos = new Vector2(e.mousePosition.x, e.mousePosition.y);
            
            Handles.DrawBezier(
                startPos,
                endPos,
                startPos,
                endPos,
                Color.green,
                null,
                2f
            );
            
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
        if (currentTree == null) return;

        DialogueNode newNode = new DialogueNode
        {
            nodeID = $"node_{System.Guid.NewGuid().ToString().Substring(0, 8)}",
            dialogueText = "Enter dialogue text here...",
            graphPosition = new Rect(100 + nodes.Count * 30, 100 + nodes.Count * 30, 350, 200) // Increased width
        };

        currentTree.nodes.Add(newNode);
        
        if (string.IsNullOrEmpty(currentTree.startNodeID))
        {
            currentTree.startNodeID = newNode.nodeID;
        }

        EditorUtility.SetDirty(currentTree);
        GUI.changed = true;
    }

    private void DeleteNode(DialogueNode nodeToDelete)
    {
        if (nodes.Count <= 1) return;

        // Remove all references to this node
        foreach (var node in nodes)
        {
            for (int i = node.choices.Count - 1; i >= 0; i--)
            {
                if (node.choices[i].targetNodeID == nodeToDelete.nodeID)
                {
                    node.choices[i].targetNodeID = "";
                }
            }
        }
        
        // Update start node if needed
        if (currentTree.startNodeID == nodeToDelete.nodeID)
        {
            currentTree.startNodeID = nodes[0].nodeID;
        }
        
        nodes.Remove(nodeToDelete);
        
        if (selectedNode == nodeToDelete)
        {
            selectedNode = null;
        }

        EditorUtility.SetDirty(currentTree);
        GUI.changed = true;
    }
}