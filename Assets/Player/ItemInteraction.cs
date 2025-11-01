using System.Collections.Generic;
using UnityEngine;


public class ItemInteraction : Interaction
{
    [Header("Item fields")]
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private Dialogue dialogueMono;
    [SerializeField] private Color dialogueBoxColor;
    [SerializeField] private Color dialogueTextColor;
    [SerializeField] private DialogueResponse dialogueResponse;
    [SerializeField] bool UseItemDialogueTree = false;
    [SerializeField] private DialogueTree ItemDialogueTree;
    [SerializeField] private string SingleDialogueText = "Hello.";
    [SerializeField] private Animator item_animator;
    [SerializeField] private string item_animTriggerName;
    [SerializeField] private AudioSource item_audioSource;
    [SerializeField] private ParticleSystem item_particleSystem;
    public bool AlterInteractablePopup = false;
    public string NewPopup = "It seems to be Locked.";
    private int TriggerHash;
    public bool CanInteract = false;
    

    [Header("Alter NPC dialogue after interacting.")]
    public DialogueTree NpcDialogueTree;
    public DialogueResponse NpcDialogueResponse;
    public bool AlterDialogueChoices = false;
    public string DialogueNodeToAlter = "StartNode";
    public List<int> ChoicesToAlter; // alter choices based on sort order, if any choices are within this sort order then toggles their bool states.
    public InteractableItem interactableItem;
    public bool AlterDialogueNodeText = false;
    public string NewDialogueText = "I unlocked it for you";

    private DialogueTree clonedDialogueTree;
    void Start()
    {
        TriggerHash = Animator.StringToHash(item_animTriggerName);
    }
    public void SetState(bool state) { CanInteract = state; }
    private bool isNull()
    {
        return dialogueCanvas == null || dialogueMono == null;
    }

    private protected override void StuffToDo()
    {
        if (AlterInteractablePopup) interactableItem.ItemPopUpText = NewPopup;

        // set dialogueResponse.dialogueTree to a clone containing these altercations. the cloned scriptable asset dialoguetree should be destroyed after the game ends. 
        // dialogueTreeClone = ...
        if (AlterDialogueChoices || AlterDialogueNodeText)
        {
            if (clonedDialogueTree != null) DialogueTreeDestroyer.DestroyTree(clonedDialogueTree);
            DialogueTree clone = DialogueTreeDestroyer.CloneDialogueTree(NpcDialogueTree);
            clonedDialogueTree = clone;

            //alter
            ApplyDialogueAlterations(clonedDialogueTree);

            //set
            NpcDialogueResponse.NPCDialogueTree = clonedDialogueTree;
        }

        if (AlterDialogueChoices || AlterInteractablePopup || AlterDialogueNodeText) { AlterDialogueNodeText = false; AlterDialogueChoices = false; AlterInteractablePopup = false; return; }

        if (isNull() || !CanInteract) return;
        if (!dialogueMono.gameObject.activeSelf)
        {
            dialogueCanvas.enabled = true;
            dialogueMono.gameObject.SetActive(true);

            // Note: for items that have multiple actions you can take. or for those with none do a single description/inspect upon interact.
            if (UseItemDialogueTree) dialogueResponse.InitializeDialogue(ItemDialogueTree);
            else { dialogueMono.SetDialogue(SingleDialogueText, InteractAgent.Interact_Key); dialogueMono.PlayNext(); }

            item_animator.SetTrigger(TriggerHash);
            item_particleSystem.Play();
        }
    }
    /// <summary>
    /// Applies alterations to the cloned dialogue tree based on the inspector settings
    /// </summary>
    private void ApplyDialogueAlterations(DialogueTree tree)
    {
        if (AlterDialogueNodeText && !string.IsNullOrEmpty(DialogueNodeToAlter))
        {
            DialogueNode nodeToAlter = tree.GetNode(DialogueNodeToAlter);
            if (nodeToAlter != null)
            {
                nodeToAlter.dialogueText = NewDialogueText;
            }
        }

        if (AlterDialogueChoices && ChoicesToAlter != null && ChoicesToAlter.Count > 0)
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
