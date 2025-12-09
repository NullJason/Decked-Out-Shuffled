using UnityEngine;

/* An Interactable can be interacted with. It will try to set itself up to interact with some InteractAgent. 
 */
public class Interactable : MonoBehaviour{
	[SerializeField] Interaction interaction;
	[SerializeField] InteractAgent agent;
	[SerializeField, Range(0, 100)] float interactionDistance = 5;  
	public float InteractDist => interactionDistance;
	
	private void Start(){
		AddToAgent();
	}

	public void Interact(){
		if (interaction != null) interaction.Trigger(); else Debug.Log("No Interaction avaliable.");
	}

	public virtual void OnOver(){
		//TODO! Maybe add some visual effect?
	}
	public virtual void OnNotOver(){
		
	}

	//By default, an Interactable will automatically add itself to the default Interaction Agent. 
	//Otherwise, if an Interact Agent is specified, this Interactable will be added to that Interaction Agent. 
	private protected void AddToAgent(){
		if(agent == null) InteractAgent.AddToDefault(this);
		else agent.Add(this);
	}
}
