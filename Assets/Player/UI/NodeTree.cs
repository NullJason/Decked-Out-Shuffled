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
public class NodeTree<T> : ScriptableObject, INodeTree where T : INode, new()
{
    public List<T> nodes = new List<T>();
    public string startNodeID;
    
    public List<INode> Nodes 
    { 
        get 
        {
            var result = new List<INode>();
            foreach (var node in nodes) result.Add(node);
            return result;
        }
    }
    
    public string StartNodeID { get => startNodeID; set => startNodeID = value; }
    
    public INode GetNode(string id) => nodes.Find(n => n.NodeID == id);
    
    public T GetTypedNode(string id) => nodes.Find(n => n.NodeID == id);
}