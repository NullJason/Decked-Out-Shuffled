using UnityEngine;
using System.Collections.Generic;


// testing to see if generic node editor is possible
[System.Serializable]
public class DialogueINode : INode
{
    public string nodeID;
    public string dialogueText;
    public int characterHeadshotID;
    public List<DialogueChoice> choices = new List<DialogueChoice>();
    public Rect graphPosition;

    public string NodeID { get => nodeID; set => nodeID = value; }
    public Rect GraphPosition { get => graphPosition; set => graphPosition = value; }

    public List<string> GetConnectedNodeIDs()
    {
        var connectedIDs = new List<string>();
        foreach (var choice in choices)
        {
            if (!string.IsNullOrEmpty(choice.targetNodeID))
                connectedIDs.Add(choice.targetNodeID);
        }
        return connectedIDs;
    }

    public List<DialogueChoice> EnabledChoices
    {
        get
        {
            List<DialogueChoice> enabledChoices = new List<DialogueChoice>();
            foreach (DialogueChoice dc in choices)
            {
                if (dc.enabled) enabledChoices.Add(dc);
            }
            return enabledChoices;
        }
    }
}

[CreateAssetMenu(fileName = "DialogueINodeTree", menuName = "Dialogue/DialogueINodeTree")]
public class DialogueINodeTree : ScriptableObject
{
    public string startNodeID;
    public List<DialogueINode> nodes = new List<DialogueINode>();
    public DialogueINode GetNode(string id) => nodes.Find(n => n.nodeID == id);
}