using System.Collections.Generic;
using UnityEngine;
public class ActionManager : MonoBehaviour
{
    [System.Serializable]
    public class ActionMapping
    {
        public string actionID;
        public EventAction eventAction;
    }
    
    public List<ActionMapping> actionMappings = new List<ActionMapping>();
    
    public void ExecuteAction(string actionID)
    {
        var mapping = actionMappings.Find(m => m.actionID == actionID);
        if (mapping != null && mapping.eventAction != null)
        {
            Debug.Log("Found action");
            mapping.eventAction.DoEventAction();
        } else Debug.Log($"DID NOT FIND ACTION [{actionID}]");
    }
}
