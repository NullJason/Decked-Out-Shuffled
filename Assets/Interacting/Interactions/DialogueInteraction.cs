using UnityEngine;

/* An Interaction that can be used to trigger dialogue using a specified instance of the Dialogue class. 
 */ 
public class DialogueInteraction : Interaction
{
	[SerializeField] private Dialogue dialogue;
	[SerializeField] string text;
	private protected override void StuffToDo(){
		if(dialogue != null){
			dialogue.QueueDialogue(text, KeyCode.Return);
			dialogue.PlayNext();
		}
		else Debug.LogWarning("No dialogue attached to DialogueInteraction " + gameObject.name);
	}
}
