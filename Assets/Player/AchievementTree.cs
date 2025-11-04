using System.Collections.Generic;
using UnityEngine;
public class AchievementNode : INode
{
    public string nodeID; // id for this node
    public Sprite AchievementIcon; // icon sprite displayed
    public Sprite IconBorder; // fancy stuff behind and around the icon ig
    public string TitleText; // title of achievement
    public string DescriptionText; // any text u like under the title
    public bool IsObtained;
    public EventAction AchievementAction; // stuff that happens after a achievement, ex: unlock a secret door. 
    public bool isUnlocked = true; // is achievement obtainable
    public List<string> NextAchievements; // a list of achievement ids: achievements that become obtainable/unlocked after this one is obtained.
    public Rect graphPosition;
    
    // INode implementation
    public string NodeID { get => NodeID; set => NodeID = value; }
    public Rect GraphPosition { get => graphPosition; set => graphPosition = value; }
    
    public List<string> GetConnectedNodeIDs() => NextAchievements ?? new List<string>();
}

// [CreateAssetMenu(fileName = "AchievementTree", menuName = "Achievement/Achievement Tree")]
// public class AchievementTree : ScriptableObject
// {
//     public List<AchievementNode> nodes = new List<AchievementNode>();
//     public AchievementNode GetNode(string id) => nodes.Find(n => n.NodeID == id);
//     public void WipeAchievementProgress() { foreach (AchievementNode an in nodes) { an.IsObtained = false; } }
// }
[CreateAssetMenu(fileName = "AchievementTree", menuName = "Achievement/Achievement Tree")]
public class AchievementTree : NodeTree<AchievementNode>
{
    public void WipeAchievementProgress()
    {
        foreach(AchievementNode an in nodes) { 
            an.IsObtained = false; 
        }
    }
}