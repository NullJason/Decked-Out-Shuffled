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
    public List<DialogueChoice> EnabledChoices
    {
        get {
            List<DialogueChoice> enabledChoices = new List<DialogueChoice>();
            foreach (DialogueChoice dc in choices)
            {
                if (dc.enabled) enabledChoices.Add(dc);
            }
            return enabledChoices;
        }
    }
}

[System.Serializable]
public class DialogueChoice
{
    public int sortOrder; 
    public string choiceText;
    public string targetNodeID;
    public string buttonAction;
    public bool enabled = true;
}

[CreateAssetMenu(fileName = "DialogueTree", menuName = "Dialogue/Dialogue Tree")]
public class DialogueTree : ScriptableObject
{
    public string startNodeID;
    public List<DialogueNode> nodes = new List<DialogueNode>();
    public DialogueNode GetNode(string id) => nodes.Find(n => n.nodeID == id);
}