using UnityEngine;

public class GiveCard : Interaction
{
	[SerializeField] Inventory inv;
	[SerializeField] Card[] cards;
	private protected override void StuffToDo(){
		foreach(Card card in cards){
			if(card != null) inv.AddCard(card);
			else Debug.LogError("Adding null card failed!");
		}
	}
}
