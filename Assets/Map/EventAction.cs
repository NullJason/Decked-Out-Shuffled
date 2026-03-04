using System.Collections;
using UnityEngine;

/// <summary>
/// this is most used to activate monobehaviors which can't be set up or referenced normally
/// due to contraints such as dialogue trees due to being a scriptable object.
/// 
/// Requires a ActionManager to use.
/// </summary>
public abstract class EventAction : MonoBehaviour
{
    private string _actionID = null;
    
    public string ActionID
    {
        get
        {
            if (_actionID == null)
            {
                _actionID = GetType().Name;
            }
            return _actionID;
        }
    }
    
    protected virtual void Awake()
    {
        ActionManager.AddAction(this);
    }
    
    protected virtual void OnEnable()
    {
        ActionManager.AddAction(this);
    }
    
    protected virtual void OnDisable()
    {
        ActionManager.RemoveAction(ActionID);
    }
    
    protected virtual void OnDestroy()
    {
        ActionManager.RemoveAction(ActionID);
    }
    
    public abstract void DoEventAction();
    
}