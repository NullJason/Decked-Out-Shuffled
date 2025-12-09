using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class ItemInteraction : Interaction
{
    [Header("Item Popup")]
    [SerializeField] private Canvas ItemPopupDialogueCanvas; // small talk canvas
    [SerializeField] private Dialogue ItemPopupDialogueMono;
    [SerializeField] private bool AlterInteractablePopupOnTryInteract = false;
    [SerializeField] private InteractableItem interactableItem;
    [SerializeField] private string NewPopup = "It seems to be Locked.";
    [Header("Obtained after Interact")]
    [SerializeField] private List<string> ItemNames = new List<string>();
    [SerializeField] private List<int> ItemCounts = new List<int>();
    // [SerializeField] private int PopupWaitCount, amount of times to interact before popup changes

    [Header("Item Main Dialogue")]
    [SerializeField] private DialogueResponse ItemDialogueResponse;
    [SerializeField] private DialogueTree ItemDialogueTree;
    [SerializeField] private bool UseItemDialogueTree = false;
    [SerializeField] private string NewDialogueStartID;
    [SerializeField] private string ItemDialogueText = "Hello. Are you interacting with me? Thanks.";
    // [SerializeField] private int DialogueWaitCount, amount of times to interact before dialogue happens

    [Header("Item Effects")]
    [SerializeField] private Animator item_animator;
    [SerializeField] private string item_animTriggerName;
    [SerializeField] private AudioSource item_audioSource;
    [SerializeField] private ParticleSystem item_particleSystem;
    
    private int TriggerHash;
    public bool CanInteract = false;


    [Header("Alter NPC dialogue after interacting.")]
    [SerializeField] private bool AlterNpcDialogueOnTryInteract = false;
    // [SerializeField] private bool AlterNpcDialogueOnInteractPassed = false;
    [SerializeField] private DialogueResponse NpcDialogueResponse;
    [SerializeField] private string NewStartNodeID;
    [SerializeField] private string EventActionOnTry;
    private ActionManager eventManager;

    void Start()
    {
        eventManager = FindFirstObjectByType<ActionManager>();
        TriggerHash = Animator.StringToHash(item_animTriggerName);
    }
    public void SetState(bool state) { CanInteract = state; }
    private bool isNull()
    {
        return ItemPopupDialogueCanvas == null || ItemPopupDialogueMono == null;
    }

    private protected override void StuffToDo()
    {
        if (AlterInteractablePopupOnTryInteract) interactableItem.ItemPopUpText = NewPopup;

        if (AlterNpcDialogueOnTryInteract)
        {
            NpcDialogueResponse.SetDefaultStartNode(NewStartNodeID);
        }

        eventManager.ExecuteAction(EventActionOnTry);

        if (AlterNpcDialogueOnTryInteract || AlterInteractablePopupOnTryInteract) { AlterNpcDialogueOnTryInteract = false; AlterInteractablePopupOnTryInteract = false; }

        
        if (isNull() || !CanInteract) return;
        
        if(ItemNames.Count > 0 && ItemCounts.Count == ItemNames.Count) FindFirstObjectByType<Player>()
            .ObtainItems(ItemNames.Zip(ItemCounts, (key, value) => new { key, value })
            .ToDictionary(item => item.key, item => item.value));

        // Note: for items that have multiple actions you can take. or for those with none do a single description/inspect upon interact.
        if (UseItemDialogueTree) ItemDialogueResponse.InitializeDialogue(ItemDialogueTree);
        else { Dialogue.DefaultDialogueMono.Play(ItemDialogueText); ItemPopupDialogueMono.PlayNext(); }

        if (item_animator != null && TriggerHash != 0) item_animator.SetTrigger(TriggerHash);
        if (item_particleSystem != null) item_particleSystem.Play();
        if (item_audioSource != null) item_audioSource.Play();
            
        
    }
}
