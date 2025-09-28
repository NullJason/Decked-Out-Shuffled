using UnityEngine;

public class SmallTalkInteraction : Interaction
{
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private Dialogue dialogueMono;
    private bool isNull()
    {
        return dialogueCanvas == null || dialogueMono == null;
    }

    /// <summary>
    /// enable main dialog box, as long as the dialog box is currently open do not trigger again.
    /// </summary>
    private protected override void StuffToDo()
    {
        Debug.Log("Enabling main dialog from small talk.");
        if (isNull()) return;
        if (!dialogueMono.gameObject.activeSelf)
        {
            dialogueCanvas.enabled = true;
            dialogueMono.gameObject.SetActive(true);    
        }
    }
}
