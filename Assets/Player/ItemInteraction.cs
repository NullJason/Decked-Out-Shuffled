using System.Collections.Generic;
using UnityEngine;


public class ItemInteraction : Interaction
{
    [Header("Item Popup")]
    [SerializeField] private Canvas ItemPopupDialogueCanvas; // small talk canvas
    [SerializeField] private Dialogue ItemPopupDialogueMono;
    [SerializeField] private bool AlterInteractablePopupOnTryInteract = false;
    [SerializeField] private InteractableItem interactableItem;
    [SerializeField] private string NewPopup = "It seems to be Locked.";
    // [SerializeField] private int PopupWaitCount, amount of times to interact before popup changes

    [Header("Item Main Dialogue")]
    [SerializeField] private Dialogue MainDialogue;
    [SerializeField] private DialogueResponse ItemDialogueResponse;
    [SerializeField] private bool UseItemDialogueTree = false;
    [SerializeField] private DialogueTree ItemDialogueTree;
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
    [SerializeField] private DialogueTree NpcDialogueTree;
    [SerializeField] private DialogueResponse NpcDialogueResponse;
    [SerializeField] private string DialogueNodeToAlter = "StartNode";
    [SerializeField] private string NewStartNodeID;
    [SerializeField] private List<int> ChoicesToAlter; // alter choices based on sort order, if any choices are within this sort order then toggles their bool states.
    [SerializeField] private string NewNodeDialogueText = "I unlocked it for you";

    private DialogueTree clonedDialogueTree;
    void Start()
    {
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
            if (clonedDialogueTree != null) ApplyDialogueAlterations(clonedDialogueTree);
            else
            {
                // gonna consider using obj pooling for SOs clones so it doesn't overwrite npc DT behavior in the case there are multpiple items altering the tree.
                clonedDialogueTree = DialogueTreeDestroyer.CloneDialogueTree(NpcDialogueTree);

                ApplyDialogueAlterations(clonedDialogueTree);

                NpcDialogueResponse.NPCDialogueTree = clonedDialogueTree;
            }
        }

        if (AlterNpcDialogueOnTryInteract || AlterInteractablePopupOnTryInteract) { AlterNpcDialogueOnTryInteract = false; AlterInteractablePopupOnTryInteract = false; }

        
        
        if (isNull() || !CanInteract) return;
        

        // Note: for items that have multiple actions you can take. or for those with none do a single description/inspect upon interact.
        if (UseItemDialogueTree) ItemDialogueResponse.InitializeDialogue(ItemDialogueTree);
        else { MainDialogue.SetDialogue(ItemDialogueText, InteractAgent.Interact_Key); ItemPopupDialogueMono.PlayNext(); }

        if (item_animator != null && TriggerHash != 0) item_animator.SetTrigger(TriggerHash);
        if (item_particleSystem != null) item_particleSystem.Play();
        if (item_audioSource != null) item_audioSource.Play();
            
        
    }
    /// <summary>
    /// Applies alterations to the cloned dialogue tree based on the inspector settings
    /// </summary>
    private void ApplyDialogueAlterations(DialogueTree tree)
    {
        if (!AlterNpcDialogueOnTryInteract) return;
        if (!string.IsNullOrEmpty(DialogueNodeToAlter))
        {
            DialogueNode nodeToAlter = tree.GetNode(DialogueNodeToAlter);
            if (nodeToAlter != null)
            {
                nodeToAlter.dialogueText = NewNodeDialogueText;
            }
        }
        if (!string.IsNullOrEmpty(NewStartNodeID)) tree.startNodeID = NewStartNodeID;
        if (ChoicesToAlter != null && ChoicesToAlter.Count > 0)
        {
            DialogueNode nodeToAlter = tree.GetNode(DialogueNodeToAlter);
            if (nodeToAlter != null)
            {
                foreach (int sortOrder in ChoicesToAlter)
                {
                    DialogueChoice choiceToAlter = nodeToAlter.choices.Find(c => c.sortOrder == sortOrder);
                    if (choiceToAlter != null)
                    {
                        choiceToAlter.enabled = !choiceToAlter.enabled;
                    }
                }
            }
        }
    }
}
