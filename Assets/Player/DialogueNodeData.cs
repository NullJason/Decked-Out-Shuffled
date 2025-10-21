using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueNodeData
{
    public string nodeID;
    public string buttonText;
    public string responseText;
    public int characterHeadshotID;
    public bool isPrimary;
    public List<string> nextNodeIDs = new List<string>();
    public Rect graphPosition = new Rect(100, 100, 250, 200);
}