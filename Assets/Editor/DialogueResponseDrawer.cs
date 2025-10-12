using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(DialogueResponse))]
public class DialogueResponseDrawer : PropertyDrawer
{
    private const float LINE_HEIGHT = 18f;
    private const float VERTICAL_SPACING = 2f;
    private const float HORIZONTAL_SPACING = 5f;
    private const float INDENT_WIDTH = 15f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty dialogueList = property.FindPropertyRelative("Dialogue");
        int lineCount = 1; // Header
        
        if (dialogueList.isExpanded)
        {
            lineCount += 1; // Size field
            
            // Only show parallel lists if we have elements
            if (dialogueList.arraySize > 0)
            {
                lineCount += 1; // Headers
                lineCount += dialogueList.arraySize; // List elements
            }
            
            // ResponseIDs section
            SerializedProperty responseIDsList = property.FindPropertyRelative("ResponseIDs");
            lineCount += 1; // ResponseIDs header
            if (responseIDsList.isExpanded)
            {
                lineCount += 1; // Size field
                lineCount += responseIDsList.arraySize; // List elements
            }
        }
        
        return lineCount * (LINE_HEIGHT + VERTICAL_SPACING);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty dialogueList = property.FindPropertyRelative("Dialogue");
        SerializedProperty optionIDList = property.FindPropertyRelative("OptionID");
        SerializedProperty responseIDsList = property.FindPropertyRelative("ResponseIDs");

        Rect rect = new Rect(position.x, position.y, position.width, LINE_HEIGHT);
        
        // Draw main foldout
        dialogueList.isExpanded = EditorGUI.Foldout(rect, dialogueList.isExpanded, label, true);
        
        if (dialogueList.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            // Draw array size
            rect.y += LINE_HEIGHT + VERTICAL_SPACING;
            EditorGUI.PropertyField(rect, dialogueList.FindPropertyRelative("Array.size"), new GUIContent("Size"));
            
            // Synchronize lists after size might have changed
            SynchronizeLists(dialogueList, optionIDList);

            // Only draw parallel lists if we have elements
            if (dialogueList.arraySize > 0)
            {
                rect.y += LINE_HEIGHT + VERTICAL_SPACING;
                DrawParallelLists(rect, dialogueList, optionIDList, "Dialogue", "OptionID");
            }

            // Draw ResponseIDs list below
            rect.y += GetParallelListsHeight(dialogueList);
            DrawResponseIDsList(rect, responseIDsList);
            
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    private void DrawParallelLists(Rect position, SerializedProperty list1, SerializedProperty list2, string label1, string label2)
    {
        // Calculate widths for parallel display
        float labelWidth = 60f;
        float fieldWidth = (position.width - labelWidth - HORIZONTAL_SPACING) / 2f;

        // Draw headers
        Rect headerRect = new Rect(position.x, position.y, position.width, LINE_HEIGHT);
        Rect label1Rect = new Rect(headerRect.x + labelWidth, headerRect.y, fieldWidth, LINE_HEIGHT);
        Rect label2Rect = new Rect(label1Rect.x + fieldWidth + HORIZONTAL_SPACING, headerRect.y, fieldWidth, LINE_HEIGHT);

        EditorGUI.LabelField(label1Rect, label1);
        EditorGUI.LabelField(label2Rect, label2);

        // Draw list elements
        for (int i = 0; i < list1.arraySize; i++)
        {
            float yPos = position.y + (i + 1) * (LINE_HEIGHT + VERTICAL_SPACING);
            
            // Index label
            Rect indexRect = new Rect(position.x, yPos, labelWidth, LINE_HEIGHT);
            EditorGUI.LabelField(indexRect, $"Element {i}");
            
            // Dialogue field
            Rect field1Rect = new Rect(position.x + labelWidth, yPos, fieldWidth, LINE_HEIGHT);
            SerializedProperty element1 = list1.GetArrayElementAtIndex(i);
            if (element1 != null)
            {
                EditorGUI.PropertyField(field1Rect, element1, GUIContent.none);
            }
            
            // OptionID field
            Rect field2Rect = new Rect(field1Rect.x + fieldWidth + HORIZONTAL_SPACING, yPos, fieldWidth, LINE_HEIGHT);
            SerializedProperty element2 = list2.GetArrayElementAtIndex(i);
            if (element2 != null)
            {
                EditorGUI.PropertyField(field2Rect, element2, GUIContent.none);
            }
        }
    }

    private void DrawResponseIDsList(Rect position, SerializedProperty list)
    {
        // Draw ResponseIDs foldout
        list.isExpanded = EditorGUI.Foldout(position, list.isExpanded, "Response IDs", true);
        
        if (list.isExpanded)
        {
            // Add extra indentation
            Rect indentedRect = new Rect(position.x + INDENT_WIDTH, position.y + LINE_HEIGHT + VERTICAL_SPACING, 
                                       position.width - INDENT_WIDTH, LINE_HEIGHT);
            
            // Draw array size
            EditorGUI.PropertyField(indentedRect, list.FindPropertyRelative("Array.size"), new GUIContent("Size"));
            
            // Draw list elements
            for (int i = 0; i < list.arraySize; i++)
            {
                indentedRect.y += LINE_HEIGHT + VERTICAL_SPACING;
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                if (element != null)
                {
                    EditorGUI.PropertyField(indentedRect, element, new GUIContent($"Response {i}"));
                }
            }
        }
    }

    private float GetParallelListsHeight(SerializedProperty list)
    {
        if (list.arraySize == 0)
            return 0f;
        
        return (list.arraySize + 1) * (LINE_HEIGHT + VERTICAL_SPACING); // +1 for headers
    }

    private void SynchronizeLists(SerializedProperty list1, SerializedProperty list2)
    {
        // Only synchronize if sizes are different
        if (list1.arraySize != list2.arraySize)
        {
            list2.arraySize = list1.arraySize;
        }
    }
}