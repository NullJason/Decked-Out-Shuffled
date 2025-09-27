using UnityEngine;

/*When an Interactable is interacted with by an InteractAgent, the Interactable will trigger some Interaction. 
 */
abstract public class Interaction : MonoBehaviour {
	[SerializeField] Interaction next;
	public void Trigger(){
		StuffToDo();
		if(next != null) next.Trigger();
	}
	abstract private protected void StuffToDo();
}
