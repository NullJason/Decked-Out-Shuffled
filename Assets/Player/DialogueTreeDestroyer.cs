using System.Collections.Generic;
using UnityEngine;

public class DialogueTreeDestroyer : MonoBehaviour
{
    [Header("This Manages Clones and destroys Clones of DialogueTree SOs to prevent clogigng up the asset folder after game ends.")]
    public bool DoDestroyState = true;
    public static List<DialogueTree> trees = new List<DialogueTree>();
    public static void AddTree(DialogueTree t) { trees.Add(t); }
    /// <summary>
    /// Creates a deep copy of a DialogueTree ScriptableObject
    /// </summary>
    public static DialogueTree CloneDialogueTree(DialogueTree original)
    {
        DialogueTree clone = ScriptableObject.CreateInstance<DialogueTree>();
        clone.startNodeID = original.startNodeID;
        clone.nodes = new List<DialogueNode>();

        foreach (DialogueNode originalNode in original.nodes)
        {
            DialogueNode clonedNode = new DialogueNode
            {
                nodeID = originalNode.nodeID,
                dialogueText = originalNode.dialogueText,
                characterHeadshotID = originalNode.characterHeadshotID,
                graphPosition = originalNode.graphPosition,
                choices = new List<DialogueChoice>()
            };

            foreach (DialogueChoice originalChoice in originalNode.choices)
            {
                DialogueChoice clonedChoice = new DialogueChoice
                {
                    sortOrder = originalChoice.sortOrder,
                    choiceText = originalChoice.choiceText,
                    targetNodeID = originalChoice.targetNodeID,
                    buttonAction = originalChoice.buttonAction,
                    enabled = originalChoice.enabled
                };
                clonedNode.choices.Add(clonedChoice);
            }

            clone.nodes.Add(clonedNode);
        }

        AddTree(clone);
        return clone;
    }
    public static void DestroyTree(DialogueTree t)
    {
        if (trees.Contains(t)) trees[trees.IndexOf(t)] = null;
        Destroy(t);
    }
    void OnDestroy()
    {
        foreach (DialogueTree t in trees)
        {
            if(t!=null) Destroy(t);
        }
    }
    void OnDisable()
    {
        foreach (DialogueTree t in trees)
        {
            if(t!=null) Destroy(t);
        }
    }
    void OnApplicationQuit()
    {
        foreach (DialogueTree t in trees)
        {
            if(t!=null) Destroy(t);
        }
    }
}
