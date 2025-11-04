
public class UnlockDoor : EventAction
{
    public ItemInteraction interaction;
    // called by DialogueResponseCache The moment the button is clicked.
    // public void DialogueButtonAction()
    // {
    //     interaction.CanInteract = true;
    // }

    public override object DoEventAction()
    {
        interaction.CanInteract = true;
        return true;
    }
}
