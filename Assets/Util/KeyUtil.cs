using UnityEngine;

//Stores references to important objects, such as the player's various scripts. 
//Attach exactly one instance of this class to some GameObject that exists for the entire length of the game, such as the player. 

public class KeyUtil : MonoBehaviour
{
	[SerializeField] private Inventory playerInventory;

//	[SerializeField] private UserInput playerInput;


	public static KeyUtil main;

	void Start(){
		if(main == null){
			main = this;
			Debug.Log("Set up key util values!");
		} 
		else Debug.LogError("Could not set up multiple key util values, but was attempted!");
	}

	public Inventory PlayerInventory() {
		Debug.Log("playerInvetory: " + playerInventory);
		return playerInventory;
	}

	public Deck PlayerDeck() {
		return playerInventory.GetDeck();
	}

	//public UserInput PlayerInput() {
	//	return playerInput;
	//}
}
