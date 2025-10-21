using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueNode
{
    public string nodeID;
    public string dialogueText;
    public int characterHeadshotID;
    public List<DialogueChoice> choices = new List<DialogueChoice>();
    public Rect graphPosition;
}

[System.Serializable]
public class DialogueChoice
{
    public int sortOrder; 
    public string choiceText;
    public string targetNodeID;
    public MonoBehaviour buttonAction;
}

[CreateAssetMenu(fileName = "DialogueTree", menuName = "Dialogue/Dialogue Tree")]
public class DialogueTree : ScriptableObject
{
    public string startNodeID;
    public List<DialogueNode> nodes = new List<DialogueNode>();
    public DialogueNode GetNode(string id) => nodes.Find(n => n.nodeID == id);
}