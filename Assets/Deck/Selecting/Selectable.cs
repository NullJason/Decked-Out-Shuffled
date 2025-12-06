using UnityEngine;
//Represents an object in a card game that a player or AI could interact with. 
public abstract class Selectable : MonoBehaviour
{
	private void Start(){
		UserInput.Add(this);
	}

	private void OnDestroy(){
		UserInput.Remove(this);
	}

	//TODO!!
	public abstract void Select();

	public abstract void Highlight();
	public abstract void Unhighlight();
}
