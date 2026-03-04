using UnityEngine;

public class ChangeStartNodeToExplodedoor : EventAction
{
    public DialogueResponse Entis_dr;
    public override void DoEventAction()
    {
        Debug.Log("Changing to explode door id");
        Entis_dr.SetDefaultStartNode("UnlockDoorPls");
    }
}
