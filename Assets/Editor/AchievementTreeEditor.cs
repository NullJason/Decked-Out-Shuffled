using UnityEditor;

[CustomEditor(typeof(AchievementTree))]
public class AchievementTreeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        AchievementTree tree = (AchievementTree)target;

        EditorGUILayout.HelpBox("Use the Node Editor window to edit this dialogue tree.", MessageType.Info);

        if (EditorGUILayout.LinkButton("Open Editor"))
        {
            GenericNodeEditor.OpenWindow();
        }

        DrawDefaultInspector();
    }
}