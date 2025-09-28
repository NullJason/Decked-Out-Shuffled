using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Initializes a Dialogue Response Button.
/// leave ID = 0, use to go back to default dialogue.
/// Must be placed under any button meant to be a response to a dialog message.
/// Use DialogueResponseHelper to organize ID's and Messages.
/// </summary>
public class DialogResponseButton : MonoBehaviour
{
    [SerializeField] private Canvas DialogueCanvas;
    [SerializeField] private Dialogue dialogueMono;
    [SerializeField] private int ButtonID;
    [SerializeField] private int ResponseID;
    private bool isNull()
    {
        return DialogueCanvas == null || dialogueMono == null;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (isNull())
        {
            DialogueCanvas = GetComponentInParent<Canvas>();
            if (DialogueCanvas != null) dialogueMono = DialogueCanvas.GetComponent<Dialogue>();
        }
        if (isNull()) return;
        dialogueMono.AddResponseButton(transform.GetComponent<Button>(), ButtonID, ResponseID);
    }

}
