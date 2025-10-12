using System.Collections.Generic;
using UnityEngine;

public class SmallTalkInteraction : Interaction
{
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private Dialogue dialogueMono;
    [SerializeField] private Color dialogueBoxColor;
    [SerializeField] private Color dialogueTextColor;
    [SerializeField] private DialogueResponse dialogueResponses;
    private bool isNull()
    {
        return dialogueCanvas == null || dialogueMono == null;
    }

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
