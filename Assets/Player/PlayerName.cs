using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerName : EventAction
{
    public TMP_InputField inputField;
    public DialogueResponse dialogueResponse;
    public DialogueResponseCache plrDRCache;
    public override void DoEventAction()
    {
        Debug.Log("Doing Player name action");
        Player.PlayerCanMove = false;
        gameObject.SetActive(true);
        inputField.onSubmit.AddListener(OnInputFieldSubmit);
        plrDRCache.SetCanvasActiveState(false);
    }
    void OnInputFieldSubmit(string text)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ValidateUsername(text);
        }
    }
    public void ValidateUsername(string username)
    {
        string pattern = @"^[a-zA-Z0-9\s]+$"; 

        if (username.Length < 15 && Regex.IsMatch(username, pattern))
        {
            FindFirstObjectByType<Player>().PlayerName = username;
            plrDRCache.SetCanvasActiveState(true);
            dialogueResponse.StartDialogueFromNode("doorDialogueNode");
            dialogueResponse.SetDefaultStartNode("GoDoorOk");
            Player.PlayerCanMove = true;
            gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("Username is invalid. < 15 chars, Only letters, numbers, and whitespace allowed.");
            // Show nope message to the user
        }
    }
}
