
using System.Collections;

public class UnlockDoor : EventAction
{
    public ItemInteraction interaction;

    public override void DoEventAction()
    {
        interaction.gameObject.SetActive(true);
        interaction.CanInteract = true;
    }
}
