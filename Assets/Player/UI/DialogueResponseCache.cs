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
    [SerializeField] private GameObject ButtonPrefab;
    [SerializeField] private Dialogue mainDialogueMono;
    [SerializeField] private Sprite MainProgressionBranch_Icon;
    [SerializeField] private Sprite DefaultProgressionBranch_Icon;
    private List<GameObject> ButtonCache = new List<GameObject>();
    private DialogueResponse CurrentDR;
    private DialogueCharacters dialogueCharacters;
    void Start()
    {
        if (ButtonPrefab == null) Debug.Log("BUTTON PREFAB HASN'T BEEN SET!");
        dialogueCharacters = FindFirstObjectByType<DialogueCharacters>();
    }
    public void SetDR(DialogueResponse dr)
    {
        CurrentDR = dr;
    }
    public void UpdateSize(DialogueNode currentNode)
    {
        int amount = currentNode.choices.Count;
        if (amount > ButtonCache.Count)
        {
            for (int i = ButtonCache.Count; i < amount; i++)
            {
                GameObject buttonObj = Instantiate(ButtonPrefab, transform);
                ButtonCache.Add(buttonObj);
            }
            UpdateResponseButtons(currentNode);
        }
        else if (amount < ButtonCache.Count)
        {
            UpdateResponseButtons(currentNode);
            for (int i = amount; i < ButtonCache.Count; i++)
            {
                // todo: need to test if uilistlayout correctly ignores inactive children and activestate and 4 sort order.
                ButtonCache[i].SetActive(false);
            }
        }
    }
    private void UpdateResponseButtons(DialogueNode currentNode)
    {
        int b = -1;
        int highestLayer = -1000;
        Image priorityButtonImg = null;
        foreach (var choice in currentNode.choices)
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
            if (highestLayer < currentButtonLayer)
            {
                priorityButtonImg = buttonObj.GetComponentInChildren<Image>();
                highestLayer = currentButtonLayer;
            }
            else buttonObj.GetComponentInChildren<Image>().sprite = DefaultProgressionBranch_Icon;
        }
        if(priorityButtonImg != null) priorityButtonImg.sprite = MainProgressionBranch_Icon;
    }
    private void OnChoiceSelected(string targetNodeID, MonoBehaviour buttonAction = null)
    {
        if (CurrentDR == null) Debug.Log("Current DialogueResponse is null, buttons won't work.");

        if (buttonAction != null)
        {
            buttonAction.Invoke("DialogueButtonAction", 0f);
        }

        if (targetNodeID == "END" || !CurrentDR.NodeLookup.ContainsKey(targetNodeID))
        {
            mainDialogueMono.ExitDialogue();
            return;
        }

        CurrentDR.StartDialogueFromNode(targetNodeID);
    }
    public void DoDialogue(string dialogueText, int characterHeadshotID)
    {
        Sprite cHeadshot = dialogueCharacters.GetImage(characterHeadshotID);
        mainDialogueMono.SetHeadshot(cHeadshot);
        mainDialogueMono.Play(dialogueText);
    }
}