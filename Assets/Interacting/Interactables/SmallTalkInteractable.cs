using UnityEngine;

public class SmallTalkInteractable : Interactable
{
    [SerializeField] private Canvas SmallTalkCanvas;
    [SerializeField] private Dialogue dialogueMono;
    [TextArea]
    [SerializeField] private string PopUpText = "Hello!";
    private bool played = false;


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

    public void TempDisable()
    {
        SmallTalkCanvas.gameObject.SetActive(false);
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
        dialogueMono.QueueDialogue(PopUpText);
        played = false;
    }
}
