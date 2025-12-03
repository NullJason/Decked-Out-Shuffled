using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Cache for buttons
/// this component should be property of main button container.
/// </summary>
public class DialogueResponseCache : MonoBehaviour
{
    [SerializeField] private GameObject MainCanvas;
    [SerializeField] private GameObject ButtonContainer;
    [SerializeField] private GameObject ButtonPrefab;
    [SerializeField] private Dialogue mainDialogueMono;
    [SerializeField] private ActionManager dialogueActionManager;
    [SerializeField] private DistanceActiveStateObject mainDialogueDistanceManager;
    private List<GameObject> ButtonCache = new List<GameObject>();
    private DialogueResponse CurrentDR;
    [SerializeField] private DialogueCharacters dialogueCharacters;
    private float WaitForTextFinishTimeoutDuration = 15f; // dialogue shouldn't play this long in the first place. will immediately execute button method if passed.
    [SerializeField] private UIListLayout listLayout;
    void OnEnable()
    {
        if (ButtonPrefab == null) Debug.Log("BUTTON PREFAB HASN'T BEEN SET!");
        dialogueCharacters = FindFirstObjectByType<DialogueCharacters>();
        if (dialogueActionManager == null) dialogueActionManager = FindFirstObjectByType<ActionManager>();
        if (listLayout == null) listLayout = GetComponentInChildren<UIListLayout>();
        if (ButtonContainer == null) ButtonContainer = listLayout.gameObject; 
    }
    public void SetDR(DialogueResponse dr)
    {
        CurrentDR = dr;
    }
    public void SetContainerActiveState(bool state, Transform npc = null)
    {
        ButtonContainer.SetActive(state);

        if(mainDialogueDistanceManager!=null && state == true && npc != null)
        {
            mainDialogueDistanceManager.SetNew(Player.Player_Transform, npc, MainCanvas, true);
        }
    }
    public void SetCanvasActiveState(bool state = false){MainCanvas.SetActive(state);}
    public void UpdateSize(DialogueNode currentNode)
    {
        List<DialogueChoice> enabledChoices = currentNode.EnabledChoices;
        int amount = enabledChoices.Count;
        if (amount > ButtonCache.Count)
        {
            Debug.Log($"Adding more buttons because of node {currentNode.nodeID}");
            for (int i = ButtonCache.Count; i < amount; i++)
            {
                GameObject buttonObj = Instantiate(ButtonPrefab, ButtonContainer.transform);
                ButtonCache.Add(buttonObj);
            }
            UpdateResponseButtons(enabledChoices);
        }
        else if (amount < ButtonCache.Count)
        {
            Debug.Log($"Removing buttons because of node {currentNode.nodeID}");
            UpdateResponseButtons(enabledChoices);
            for (int i = amount; i < ButtonCache.Count; i++)
            {
                ButtonCache[i].SetActive(false);
            }
        }
        else
        {
            Debug.Log($"same amount of buttons, {currentNode.nodeID}: {amount} Button Cached {ButtonCache.Count}");
            UpdateResponseButtons(enabledChoices);
        }
    }
    private void UpdateResponseButtons(List<DialogueChoice> enabledChoices)
    {
        int b = -1;
        int highestLayer = -1000;
        ButtonPriorityImg priorityButtonImg = null;
        foreach (var choice in enabledChoices)
        {
            b++;
            GameObject buttonObj = ButtonCache[b];
            buttonObj.SetActive(true);
            Button button = buttonObj.GetComponent<Button>();
            TMP_Text text = buttonObj.GetComponentInChildren<TMP_Text>();
            ListPriority sortorder = buttonObj.GetComponent<ListPriority>();
            sortorder.SortOrder = choice.sortOrder;
            text.text = choice.choiceText;
            string targetNode = choice.targetNodeID;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnChoiceSelected(targetNode, choice.buttonAction));

            //sortorder stuff
            int currentButtonLayer = choice.sortOrder;
            ButtonPriorityImg currentBPI = buttonObj.GetComponentInChildren<ButtonPriorityImg>(true);
            if (highestLayer < currentButtonLayer)
            {
                priorityButtonImg = currentBPI;
                highestLayer = currentButtonLayer;
            }
            else currentBPI.SetButtonSideImage();
        }
        if (priorityButtonImg != null && highestLayer != 0) priorityButtonImg.SetButtonSideImage(true);
        if(listLayout != null)listLayout.ManualUpdate(); // just in case onenable doesnt call for some weird reason, (it happened before)
    }
    private void OnChoiceSelected(string targetNodeID, string buttonAction)
    {
        if (CurrentDR == null) Debug.Log("Current DialogueResponse is null, buttons won't work.");

        if (!string.IsNullOrEmpty(buttonAction))
        {
            Debug.Log("Trying to do action "+buttonAction);
            dialogueActionManager.ExecuteAction(buttonAction);
            // StartCoroutine(TriggerDialogueButtonAction(buttonAction));
        } else Debug.Log($"Button {targetNodeID} has no action, field: {buttonAction}");

        if (targetNodeID == "END" || !CurrentDR.NodeLookup.ContainsKey(targetNodeID))
        {
            mainDialogueMono.ExitDialogue();
            return;
        }

        CurrentDR.StartDialogueFromNode(targetNodeID);
    }
    IEnumerator TriggerDialogueButtonAction(string act)
    {
        float startTime = Time.time;
        while (!mainDialogueMono.FinishedDisplayingText && (Time.time - startTime < WaitForTextFinishTimeoutDuration))
        {
            yield return null;
            Debug.Log("uhhh mono hasn't finished");
        }
        dialogueActionManager.ExecuteAction(act);
    }
    public void DoDialogue(string dialogueText, int characterHeadshotID)
    {
        if (dialogueCharacters != null)
        {
            Sprite cHeadshot = dialogueCharacters.GetImage(characterHeadshotID);
            mainDialogueMono.SetHeadshot(cHeadshot);
        }
        mainDialogueMono.Play(dialogueText, -1, true);
    }
}