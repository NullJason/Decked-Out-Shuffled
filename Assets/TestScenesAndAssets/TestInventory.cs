using System.Collections.Generic;
using UnityEngine;

//Assuming the test scene has been set up properly, there should be a panel somewhere with a few toggles in a column. Pressing "Fire2" will cause all the cards with true toggles to be printed, and then remove all toggles. 
public class TestInventory : MonoBehaviour
{
	[SerializeField] Inventory inv;
	[SerializeField] List<Card> cards;
	void Start(){
		foreach(Card c in cards) {
			inv.AddCard(c);
		}
		inv.PrintCards();

		inv.StartSelection();
	}
	void Update(){
		if(Input.GetButtonDown("Fire2")){
			foreach(Card c in inv.ReapCards()) Debug.Log(c);
		}
	}
}
