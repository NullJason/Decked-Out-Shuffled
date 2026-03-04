using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemInteract : Interaction
{
    public string textDialogue;
    [Header("Obtained after Interact")]
    [SerializeField] private List<string> ItemNames = new List<string>();
    [SerializeField] private List<int> ItemCounts = new List<int>();
    // [SerializeField] private int PopupWaitCount, amount of times to interact before popup changes

    

    // [Header("Alter NPC dialogue after interacting.")]
    // [SerializeField] private bool AlterNpcDialogueOnInteractPassed = false;
    // [SerializeField] private DialogueResponse NpcDialogueResponse;
    // [SerializeField] private string NewStartNodeID;
    [SerializeField] private List<string> EventActionOnTry = new List<string>();
    private int indexAction = 0;
    private ActionManager eventManager;

    void Start()
    {
        eventManager = FindFirstObjectByType<ActionManager>();
    }
    private protected override void StuffToDo()
    {
        if(!string.IsNullOrEmpty(textDialogue)){Dialogue.DefaultDialogueMono.gameObject.SetActive(true); Dialogue.DefaultDialogueMono.Play(textDialogue); Invoke("disableMain",3);}
        // if(!string.IsNullOrEmpty(NewStartNodeID)){
           
        //     NpcDialogueResponse.SetDefaultStartNode(NewStartNodeID);}
        
        if(indexAction < EventActionOnTry.Count){eventManager.ExecuteAction(EventActionOnTry[indexAction]); Debug.Log("doing");}
        indexAction++;

        if(ItemNames.Count > 0 && ItemCounts.Count == ItemNames.Count) FindFirstObjectByType<Player>()
            .ObtainItems(ItemNames.Zip(ItemCounts, (key, value) => new { key, value })
            .ToDictionary(item => item.key, item => item.value));
        
    }
    private void disableMain()
    {
        Dialogue.DefaultDialogueMono.gameObject.SetActive(false);
    }
}
