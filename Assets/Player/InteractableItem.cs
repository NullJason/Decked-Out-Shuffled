using UnityEngine;

public class InteractableItem : Interactable
{
    [SerializeField] private Canvas SmallTalkCanvas;
    [SerializeField] private Dialogue dialogueMono;
    [TextArea]
    public static string PopUpText = "Interact [Space]";
    private bool played = false;

    public string ItemPopUpText {get{ return PopUpText; } set{ PopUpText = value; dialogueMono.SetDialogue(PopUpText); dialogueMono.PlayNext(); }}

    void Start()
    {
        AddToAgent();
        Debug.Log(isNull());
        if (isNull()) return;
        dialogueMono.QueueDialogue(PopUpText);
    }
    private bool isNull()
    {
        return SmallTalkCanvas == null || dialogueMono == null;
    }


    public override void OnOver()
    {
        if (isNull() || played) return;
        SmallTalkCanvas.gameObject.SetActive(true);
        dialogueMono.PlayNext();
        played = true;

    }
    public override void OnNotOver()
    {
        if (isNull() || !played) return;
        SmallTalkCanvas.gameObject.SetActive(false);
        dialogueMono.SetDialogue(PopUpText);
        played = false;
    }
}
