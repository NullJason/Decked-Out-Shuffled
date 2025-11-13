using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class AchievementNode : INode
{
    [SerializeField] public string nodeID;
    [SerializeField] public Sprite AchievementIcon;
    [SerializeField] public Sprite IconBorder;
    [SerializeField] public string TitleText;
    [SerializeField] public string DescriptionText;
    [SerializeField] public bool IsObtained;
    [SerializeField] public string AchievementAction;
    [SerializeField] public bool isUnlocked = true;
    [SerializeField] public List<string> NextAchievements = new List<string>();
    [SerializeField] public Rect graphPosition;

    // INode implementation.
    public string NodeID { get => nodeID; set => nodeID = value; }
    public Rect GraphPosition { get => graphPosition; set => graphPosition = value; }

    public List<string> GetConnectedNodeIDs() => NextAchievements ?? new List<string>();
    public void RemoveConnection(string targetNodeID)
    {
        if (NextAchievements != null)
            NextAchievements.Remove(targetNodeID);
    }
}

[CreateAssetMenu(fileName = "AchievementTree", menuName = "Achievement/Achievement Tree")]
public class AchievementTree : NodeTree
{
    public AchievementNode GetAchievementNodeByTitle(string title)
    {
        foreach(INode n in Nodes)
        {
            AchievementNode achnode = (AchievementNode) n;
            if(achnode.TitleText.Equals(title, System.StringComparison.OrdinalIgnoreCase)) return achnode;
        }
        return null;
    }
    public void WipeAchievementProgress()
    {
        foreach(var node in Nodes) 
        { 
            if (node is AchievementNode achievementNode)
            {
                achievementNode.IsObtained = false; 
            }
        }
    }
}