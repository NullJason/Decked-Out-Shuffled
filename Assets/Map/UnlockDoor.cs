
using System.Collections;

public class UnlockDoor : EventAction
{
    public Interaction interaction;

    public override void DoEventAction()
    {
        interaction.gameObject.SetActive(true);
    }
}
