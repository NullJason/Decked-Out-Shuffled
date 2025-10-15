using System.Collections.Generic;
using UnityEngine;

public class VisibleDeck : DeckPreset
{
	[SerializeField] private Vector3 delta;

	//TODO: On Awake, if delta is null maybe set it to a vector based on the width of one card? 

	public override void Apply(List<Card> cards, Deck deck){

		//Show all Cards and set their Transforms. 
		for(int i = 0; i < cards.Count; i++){
			cards[i].FaceFlip(Card.Face.up);
			cards[i].transform.position = transform.position + delta * i;
		}
	}
}
