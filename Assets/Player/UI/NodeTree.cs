using System;
using System.Collections.Generic;
using UnityEngine;

// Base interface for all nodes
public interface INode
{
    string NodeID { get; set; }
    Rect GraphPosition { get; set; }
    List<string> GetConnectedNodeIDs();
}

// Base interface for node trees
public interface INodeTree
{
    List<INode> Nodes { get; }
    string StartNodeID { get; set; }
    INode GetNode(string id);
}

// Generic node tree implementation
[System.Serializable]
public class NodeTree : ScriptableObject, INodeTree
{
   // for polymorphic serialization
    [SerializeReference] 
    private List<INode> _nodes = new List<INode>();
    
    public string startNodeID;

    // Public property for editor access with proper serialization
    public List<INode> Nodes => _nodes;

    public string StartNodeID { get => startNodeID; set => startNodeID = value; }

    public INode GetNode(string id) => _nodes.Find(n => n.NodeID == id);

    // Helper method to add nodes safely
    public void AddNode(INode node)
    {
        _nodes.Add(node);
    }

    // Helper method to remove nodes safely
    public void RemoveNode(INode node)
    {
        _nodes.Remove(node);
    }
}