using System.Collections.Generic;
using UnityEngine;
public static class ActionManager
{
    private static Dictionary<string, EventAction> actions = new Dictionary<string, EventAction>();
    
    private static void Initialize()
    {
        actions.Clear();
        Debug.Log("ActionManager initialized.");
    }
    
    public static void AddAction(EventAction action)
    {
        if (action == null)
        {
            Debug.LogWarning("Attempted to add null action.");
            return;
        }
        
        string id = action.ActionID;
        
        if (actions.ContainsKey(id))
        {
            Debug.LogWarning($"Multiple EventActions with class name '{id}' detected. " +
                           $"Use unique class names!" +
                           $"Overwriting previous instance.");
            actions[id] = action;
        }
        else
        {
            actions.Add(id, action);
        }
        
        Debug.Log($"Action '{id}' (type: {action.GetType().Name}) registered.");
    }
    
    public static void RemoveAction(string actionID)
    {
        if (string.IsNullOrEmpty(actionID))
        {
            Debug.LogWarning("Attempted to remove action with null/empty ID.");
            return;
        }
        
        if (actions.ContainsKey(actionID))
        {
            actions.Remove(actionID);
            Debug.Log($"Action '{actionID}' removed.");
        }
    }
    
    public static void ExecuteAction(string actionID)
    {
        if (string.IsNullOrEmpty(actionID))
        {
            Debug.LogWarning("Action ID is not specified.");
            return;
        }
        
        if (actions.TryGetValue(actionID, out EventAction action) && action != null)
        {
            Debug.Log($"Executing action: {actionID}");
            action.DoEventAction();
        }
        else
        {
            Debug.LogWarning($"Action with ID/class name '{actionID}' not found or is null.");
        }
    }
    
    public static void ExecuteAction<T>() where T : EventAction
    {
        string actionID = typeof(T).Name;
        ExecuteAction(actionID);
    }
    
    public static T GetAction<T>() where T : EventAction
    {
        string actionID = typeof(T).Name;
        if (actions.TryGetValue(actionID, out EventAction action))
        {
            return action as T;
        }
        return null;
    }
}

