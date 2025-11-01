using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueResponse : MonoBehaviour
{
    [SerializeField] private DialogueTree dialogueTree;
    [SerializeField] private DialogueResponseCache DRButtonCache; 
    private Dictionary<string, DialogueNode> nodeLookup;
    private DialogueNode currentNode;
    public DialogueTree NPCDialogueTree {get{ return dialogueTree; }set{ dialogueTree = value; }}
    public void InitializeDialogue(DialogueTree dt = null)
    {
        if (dt != null) dialogueTree = dt;

        if (dialogueTree == null)
        {
            Debug.LogError("No Dialogue Tree assigned!");
            return;
        }

        Debug.Log(dialogueTree.startNodeID);

        nodeLookup = new Dictionary<string, DialogueNode>();
        foreach (var node in dialogueTree.nodes)
        {
            nodeLookup[node.nodeID] = node;
        }

        // give the cache a ref
        DRButtonCache.SetDR(this);

        StartDialogueFromNode(dialogueTree.startNodeID);
    }

    public void StartDialogueFromNode(string nodeID)
    {
        if (nodeLookup.ContainsKey(nodeID))
        {
            currentNode = nodeLookup[nodeID];
            DisplayCurrentNode();
        }
    }
    public Dictionary<string, DialogueNode> NodeLookup{ get{ return nodeLookup; }}

    private void DisplayCurrentNode()
    {
        if (currentNode.choices.Count != 0) DRButtonCache.SetContainerActiveState(true);
        else DRButtonCache.SetContainerActiveState(false);
        DRButtonCache.DoDialogue(currentNode.dialogueText, currentNode.characterHeadshotID);
        DRButtonCache.UpdateSize(currentNode);
    }
}




