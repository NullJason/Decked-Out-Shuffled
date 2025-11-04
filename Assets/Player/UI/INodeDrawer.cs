using UnityEditor;
using UnityEngine;

public interface INodeDrawer
{
    void DrawNode(INode node, float availableWidth, System.Action onDelete);
    Vector2 CalculateRequiredSize(INode node);
}


public class DialogueINodeDrawer : INodeDrawer
{
    // Styles for the drawer
    private GUIStyle connectedPortStyle;
    private GUIStyle disconnectedPortStyle;
    
    // Callback for when a choice port is clicked
    public System.Action<DialogueINode, int> OnChoicePortClicked { get; set; }

    public DialogueINodeDrawer()
    {
        InitializeStyles();
    }

    private void InitializeStyles()
    {
        // Port styles - different colors for connected vs disconnected
        connectedPortStyle = new GUIStyle();
        connectedPortStyle.normal.background = CreateColorTexture(new Color(0f, 0.5f, 0f, 1f)); // Dark green
        connectedPortStyle.fixedWidth = 16f;
        connectedPortStyle.fixedHeight = 16f;

        disconnectedPortStyle = new GUIStyle();
        disconnectedPortStyle.normal.background = CreateColorTexture(new Color(0.5f, 0f, 0f, 1f)); // Dark red
        disconnectedPortStyle.fixedWidth = 16f;
        disconnectedPortStyle.fixedHeight = 16f;
    }

    private Texture2D CreateColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    public void DrawNode(INode node, float availableWidth, System.Action onDelete)
    {
        DialogueINode dialogueNode = node as DialogueINode;
        if (dialogueNode == null) return;

        // Node ID field
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("ID:", GUILayout.Width(30));
        dialogueNode.nodeID = EditorGUILayout.TextField(dialogueNode.nodeID);
        GUILayout.EndHorizontal();

        // Character Headshot
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Headshot ID:", GUILayout.Width(80));
        dialogueNode.characterHeadshotID = EditorGUILayout.IntField(dialogueNode.characterHeadshotID);
        GUILayout.EndHorizontal();

        // Dialogue Text
        EditorGUILayout.LabelField("Dialogue Text:");
        GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
        textAreaStyle.wordWrap = true;
        
        GUIContent textContent = new GUIContent(dialogueNode.dialogueText);
        float textHeight = textAreaStyle.CalcHeight(textContent, availableWidth - 20);
        float minTextHeight = EditorGUIUtility.singleLineHeight * 3;
        
        dialogueNode.dialogueText = EditorGUILayout.TextArea(dialogueNode.dialogueText, textAreaStyle, 
            GUILayout.Height(Mathf.Max(minTextHeight, textHeight)));

        // Choices section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Choices:", EditorStyles.boldLabel);

        // Draw choices with deletion handling
        for (int i = 0; i < dialogueNode.choices.Count; i++)
        {
            if (!DrawChoice(dialogueNode, i, availableWidth))
            {
                // Choice was deleted, break out of the loop
                break;
            }
        }

        if (GUILayout.Button("Add New Choice"))
        {
            dialogueNode.choices.Add(new DialogueChoice());
        }
    }

    private bool DrawChoice(DialogueINode node, int choiceIndex, float availableWidth)
    {
        bool choiceDeleted = false;

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
        node.choices[choiceIndex].buttonAction = EditorGUILayout.TextField(
            node.choices[choiceIndex].buttonAction
        );
        GUILayout.EndHorizontal();

        // Enabled toggle
        GUILayout.BeginHorizontal();
        node.choices[choiceIndex].enabled = EditorGUILayout.Toggle("Enabled", node.choices[choiceIndex].enabled);
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
        
        // Right-aligned controls
        GUILayout.BeginVertical(GUILayout.Width(40), GUILayout.Height(60));
        
        GUILayout.FlexibleSpace();
        
        // Output port visualization
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
        
        // Handle port click for connection
        if (Event.current.type == EventType.MouseDown && outputPortRect.Contains(Event.current.mousePosition))
        {
            OnChoicePortClicked?.Invoke(node, choiceIndex);
            Event.current.Use();
        }
        
        GUILayout.FlexibleSpace();
        
        // Delete button
        if (GUILayout.Button("X", GUILayout.Width(20)))
        {
            node.choices.RemoveAt(choiceIndex);
            choiceDeleted = true;
        }
        
        GUILayout.EndVertical(); // End right-aligned controls
        
        GUILayout.EndHorizontal(); // End choice header
        GUILayout.EndVertical(); // End choice box
        
        return !choiceDeleted;
    }

    public Vector2 CalculateRequiredSize(INode node)
    {
        DialogueINode dialogueNode = node as DialogueINode;
        if (dialogueNode == null) return new Vector2(350, 200);

        float width = 350f;
        float height = 160f;

        if (!string.IsNullOrEmpty(dialogueNode.dialogueText))
        {
            float textAreaWidth = width - 20;
            GUIContent textContent = new GUIContent(dialogueNode.dialogueText);
            float textHeight = EditorStyles.textArea.CalcHeight(textContent, textAreaWidth);
            float minTextHeight = EditorGUIUtility.singleLineHeight * 3;
            height += Mathf.Max(minTextHeight, textHeight);
        }
        else
        {
            height += EditorGUIUtility.singleLineHeight * 3;
        }

        // Add space for each choice
        height += dialogueNode.choices.Count * 120f;
        
        // Add some padding
        height += 20f;

        return new Vector2(width, height);
    }
}

public class AchievementNodeDrawer : INodeDrawer
{
    public void DrawNode(INode node, float availableWidth, System.Action onDelete)
    {
        AchievementNode achievementNode = node as AchievementNode;
        if (achievementNode == null) return;

        // Node ID
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("ID:", GUILayout.Width(30));
        achievementNode.NodeID = EditorGUILayout.TextField(achievementNode.NodeID);
        GUILayout.EndHorizontal();

        // Achievement Icon
        achievementNode.AchievementIcon = (Sprite)EditorGUILayout.ObjectField("Icon", achievementNode.AchievementIcon, typeof(Sprite), false);
        
        // Icon Border
        achievementNode.IconBorder = (Sprite)EditorGUILayout.ObjectField("Icon Border", achievementNode.IconBorder, typeof(Sprite), false);
        
        // Title Text
        achievementNode.TitleText = EditorGUILayout.TextField("Title", achievementNode.TitleText);
        
        // Description Text
        EditorGUILayout.LabelField("Description:");
        GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
        textAreaStyle.wordWrap = true;
        
        GUIContent descContent = new GUIContent(achievementNode.DescriptionText);
        float descHeight = textAreaStyle.CalcHeight(descContent, availableWidth - 20);
        achievementNode.DescriptionText = EditorGUILayout.TextArea(achievementNode.DescriptionText, textAreaStyle, 
            GUILayout.Height(Mathf.Max(60, descHeight)));

        // Toggles
        achievementNode.IsObtained = EditorGUILayout.Toggle("Is Obtained", achievementNode.IsObtained);
        achievementNode.isUnlocked = EditorGUILayout.Toggle("Is Unlocked", achievementNode.isUnlocked);
        
        // Achievement Action
        achievementNode.AchievementAction = (EventAction)EditorGUILayout.ObjectField("Action", achievementNode.AchievementAction, typeof(EventAction), true);

        // Next Achievements
        EditorGUILayout.LabelField("Next Achievements:");
        for (int i = 0; i < achievementNode.NextAchievements.Count; i++)
        {
            GUILayout.BeginHorizontal();
            achievementNode.NextAchievements[i] = EditorGUILayout.TextField(achievementNode.NextAchievements[i]);
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                achievementNode.NextAchievements.RemoveAt(i);
                i--;
            }
            GUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Add Next Achievement"))
        {
            achievementNode.NextAchievements.Add("");
        }
    }

    public Vector2 CalculateRequiredSize(INode node)
    {
        AchievementNode achievementNode = node as AchievementNode;
        float width = 400f;
        float height = 400f; // Base height

        // Calculate height based on content
        if (!string.IsNullOrEmpty(achievementNode.DescriptionText))
        {
            float descWidth = width - 20;
            GUIContent descContent = new GUIContent(achievementNode.DescriptionText);
            float descHeight = EditorStyles.textArea.CalcHeight(descContent, descWidth);
            height += Mathf.Max(60, descHeight);
        }

        height += achievementNode.NextAchievements.Count * 25f;

        return new Vector2(width, height);
    }
}