using System;
using Unity.VisualScripting;
using UnityEngine;

public class OopsiesDialogueDefaultText : MonoBehaviour
{
    string defaultOopsiesText = "Wassup, You ain't supposed to see this text right here! It seems the <wavy>Someone</wavy> got lazy! Go bother them to fix this!";
    public string oopsiesText;
    private void OnEnable() {
        Dialogue mono;
        if (!String.IsNullOrWhiteSpace(oopsiesText)) defaultOopsiesText = oopsiesText;
        if(transform.TryGetComponent<Dialogue>(out mono) && mono.GetDialogueQueueSize()==0)
        {
            mono.QueueDialogue(defaultOopsiesText);
            mono.PlayNext();
        }
    }
}
