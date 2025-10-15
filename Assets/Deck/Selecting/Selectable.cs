using UnityEngine;
//Represents an object in a card game that a player or AI could interact with. 
public abstract class Selectable : MonoBehaviour
{
	//TODO!!
	public abstract void Select();

	public abstract void Highlight();
	public abstract void Unhighlight();
}
