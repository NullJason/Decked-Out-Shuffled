using UnityEditor;

[CustomEditor(typeof(DialogueTree))]
public class DialogueTreeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DialogueTree tree = (DialogueTree)target;

        EditorGUILayout.HelpBox("Use the Dialogue Node Editor window to edit this dialogue tree.", MessageType.Info);

        if (EditorGUILayout.LinkButton("Open Dialogue Editor"))
        {
            DialogueNodeEditor.OpenWindow();
        }

        DrawDefaultInspector();
    }
}