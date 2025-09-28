using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Use this to Setup Dialogues Responses. Put in same place as Dialogue.cs
/// </summary>
public class DialogueResponseHelper : MonoBehaviour
{
    [SerializeField] private Canvas SmallTalkCanvas;
    [SerializeField] private Dialogue dialogueMono;
    [SerializeField] private List<int> ResponseID;
    [SerializeField] private List<string> ResponseMessage;
    [SerializeField] private List<KeyCode> ResponseKeyCodes;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (isNull())
        {
            SmallTalkCanvas = GetComponentInParent<Canvas>();
            if (SmallTalkCanvas != null) dialogueMono = SmallTalkCanvas.GetComponent<Dialogue>();
        }
        if (isNull()) return;

        int i = 0;
        foreach (int id in ResponseID)
        {
            dialogueMono.AddToResponse(id, ResponseMessage[i], ResponseKeyCodes[i]); i++;
        }

    }

    private bool isNull()
    {
        return SmallTalkCanvas == null || dialogueMono == null;
    }
}
