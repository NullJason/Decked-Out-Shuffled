using System.Collections.Generic;
using UnityEngine;

public class InvisibleDeck : DeckPreset
{
	public override void Apply(List<Card> cards, Deck deck){
		if(cards.Count > 0){
			//Hide all Cards and set their Transforms. 
			foreach(Card card in cards){
				card.FaceFlip(Card.Face.hidden);
				card.transform.position = transform.position;
				//card.SetSize(); TODO!!
			}
		}
	}
}
