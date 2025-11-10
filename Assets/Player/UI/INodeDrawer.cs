using UnityEditor;
using UnityEngine;

public interface INodeDrawer
{
    void DrawNode(INode node, INodeTree nodeTree, float availableWidth, System.Action onDelete);
    Vector2 CalculateRequiredSize(INode node);
}

public class AchievementNodeDrawer : INodeDrawer
{
    public System.Action<INode, int> OnChoicePortClicked { get; set; }
    public Rect LastOutputPortRect { get; private set; }
    private INodeTree _currentNodeTree;
    public void DrawNode(INode node, INodeTree nodeTree, float width, System.Action onDelete)
    {
        _currentNodeTree = nodeTree; 
        AchievementNode achievementNode = node as AchievementNode;
        if (achievementNode == null) return;

        // Draw existing achievement fields
        EditorGUILayout.LabelField("Achievement Details", EditorStyles.boldLabel);
        
        achievementNode.TitleText = EditorGUILayout.TextField("Title", achievementNode.TitleText);
        achievementNode.DescriptionText = EditorGUILayout.TextField("Description", achievementNode.DescriptionText);
        
        achievementNode.AchievementIcon = (Sprite)EditorGUILayout.ObjectField("Icon", achievementNode.AchievementIcon, typeof(Sprite), false);
        achievementNode.IconBorder = (Sprite)EditorGUILayout.ObjectField("Icon Border", achievementNode.IconBorder, typeof(Sprite), false);
        
        achievementNode.IsObtained = EditorGUILayout.Toggle("Is Obtained", achievementNode.IsObtained);
        achievementNode.isUnlocked = EditorGUILayout.Toggle("Is Unlocked", achievementNode.isUnlocked);

        // Draw Next Achievements section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Next Achievements", EditorStyles.boldLabel);
        
        // Display current connections
        if (achievementNode.NextAchievements != null && achievementNode.NextAchievements.Count > 0)
        {
           EditorGUILayout.LabelField("Connected to:");
            for (int i = achievementNode.NextAchievements.Count - 1; i >= 0; i--)
            {
                string nextId = achievementNode.NextAchievements[i];
                string nodeTitle = GetNodeTitle(nextId);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"→ {nodeTitle}");
                
                // Remove button for this connection
                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    achievementNode.RemoveConnection(nextId);
                    GUI.changed = true;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.LabelField("No connections");
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        // Output port - this will be used for drag connections
        Rect outputPortRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16), GUILayout.Height(16));
        LastOutputPortRect = outputPortRect; 
                        
        // Use your existing port styles
        bool isConnected = achievementNode.NextAchievements != null && achievementNode.NextAchievements.Count > 0;
        GUIStyle portStyle = isConnected ? 
            new GUIStyle() { normal = { background = CreateColorTexture(Color.green) } } : 
            new GUIStyle() { normal = { background = CreateColorTexture(Color.red) } };
        
        if (portStyle.normal.background != null)
        {
            GUI.Box(outputPortRect, "", portStyle);
        }
        
        // Handle port click - use -1 to indicate this is for NextAchievements (not a specific choice index)
        if (Event.current.type == EventType.MouseDown && outputPortRect.Contains(Event.current.mousePosition))
        {
            OnChoicePortClicked?.Invoke(achievementNode, -1);
            Event.current.Use();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.LabelField("Click and drag to connect to other nodes", EditorStyles.centeredGreyMiniLabel);
    }
    private string GetNodeTitle(string nodeId)
    {
        if (_currentNodeTree == null) return nodeId;
        
        INode node = _currentNodeTree.GetNode(nodeId);
        if (node is AchievementNode achievementNode)
        {
            return string.IsNullOrEmpty(achievementNode.TitleText) ? 
                $"Unnamed ({nodeId})" : 
                achievementNode.TitleText;
        }
        
        return nodeId; // Fallback to ID if not an AchievementNode
    }
    public Vector2 CalculateRequiredSize(INode node)
    {
        AchievementNode achievementNode = node as AchievementNode;
        if (achievementNode == null) return new Vector2(400, 300);
        
        float width = 400f;
        float height = 280f; 
        
        height += EditorGUIUtility.singleLineHeight * 8; 
        height += 40f; 
        
        return new Vector2(width, height);
    }
    
    private Texture2D CreateColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}