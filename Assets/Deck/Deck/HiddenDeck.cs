using System.Collections.Generic;
using UnityEngine;

public class HiddenDeck : DeckPreset
{
	[SerializeField] private bool showTop;
	public override void Apply(List<Card> cards, Deck deck){

		//Hide all Cards and set their Transforms. 
		foreach(Card card in cards){
			card.FaceFlip(Card.Face.hidden);
			card.transform.position = transform.position;
			//card.SetSize(); TODO!!
		}

		//Shows first card
		if(showTop) cards[0].FaceFlip(Card.Face.up);
		else cards[0].FaceFlip(Card.Face.down);
	}
}
