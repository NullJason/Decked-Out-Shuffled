using UnityEngine;

//Transfers all cards from one deck to the player's inventory. 
public class GiveCard : Interaction
{
	[SerializeField] Inventory inv;
	[SerializeField] Deck cards; 
	private protected void Start(){
		if(inv == null) {
			inv = KeyUtil.main.PlayerInventory();
			Debug.LogWarning("No Inventory was provided! Defaulting to the player's Inventory!");
		}
	}

	private protected override void StuffToDo(){
		inv.AddCards(cards);
	}
}
