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
		while(cards.Count() > 0) {
      			Debug.Log("Added card " + cards.GetCardAtIndex(0) + " to inventory!");
			if(cards.GetCardAtIndex(0) != null) inv.AddCard(cards.GetCardAtIndex(0), cards);
			else Debug.LogError("Adding null card failed!");
		}
	}
}
