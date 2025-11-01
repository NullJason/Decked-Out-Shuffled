using UnityEngine;

public class UnlockDoor : MonoBehaviour
{
    public ItemInteraction interaction;
    // called by DialogueResponseCache The moment the button is clicked.
    public void DialogueButtonAction()
    {
        interaction.CanInteract = true;
    }
    
}
