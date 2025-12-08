using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Deck : MonoBehaviour, IEnumerable
{
	[SerializeField]
	List<Card> cards;

	[SerializeField]
	DeckPreset preset;

	private protected void Awake(){
		foreach(Transform child in transform){
			Card card = child.GetComponent<Card>();
			if(card != null) AddCard(card);
		}
	}
	private protected void Start(){
		Render();
	}

	//Moves card1 from deck1 to a position in deck2. 
	//
	//If multiple cards in deck1 are equal to card1, only the first one will be removed. 
	//If position is zero or positive, the card will be inserted at that position of deck2. 
	//If position is negative, the card will be inserted at deck2.size + position 
	public static void MoveCard(Deck deck1, Card card1, Deck deck2, int position = 0){
		//Find the card in deck1, throw exception if doesn't exist
		int index = deck1.GetCardIndex(card1);
		if(index < 0) {
			Debug.LogError("Card " + card1.gameObject + " was not within Deck " + deck1.gameObject);
			return;
		}


		//Remove the card and insert. 
		deck2.AddCard(card1, position);
		deck1.RemoveCard(index);

		//Change the card's transform parent. 
		card1.gameObject.transform.SetParent(deck2.gameObject.transform);
	}

	//Swaps two cards in a Deck. 
	//TODO: Make a static version that allows the cards to be in different Decks!
	public void SwapCards(int cardIndex1, int cardIndex2){
		string s = "" + GetCardAtIndex(cardIndex1);
		Card temp = GetCardAtIndex(cardIndex1);
		cards[cardIndex1] = GetCardAtIndex(cardIndex2);
		cards[cardIndex2] = temp;
		Debug.Log(s + "  " + GetCardAtIndex(cardIndex1));
	}

	//Moves all Cards from moveFrom to moveTo. 
	public static void MoveCards(Deck moveFrom, Deck moveTo){
		Card c = moveFrom.GetCardAtIndex(0);
		while(c != null){
			MoveCard(moveFrom, c, moveTo);
			c = moveFrom.GetCardAtIndex(0);
		}
	}

	public Card GetCardAtIndex(int index){
		if(Count() <= 0) {
			Debug.LogWarning("Could not get card since the inventory was empty! Returning null.");
			return null;
		}
		if(index < 0) Debug.LogError("Could not get card at negative index!");
		if(index > Count()) Debug.LogError("Index " + index + " was out of bounds (" + Count() + ")!");
		return cards[index];
	}

	private int GetCardIndex(Card card){
		for(int i = 0; i < cards.Count; i++){
			if(cards[i] == card) return i;
		}
		return -1;
	}

	public int Count(){
		return cards.Count;
	}

	private void RemoveCard(int index = 0){
		index = ActualPosition(index);
		cards.RemoveAt(index);
		Render();
	}

	//This should generally not be called! If you're moving cards between two decks, do not use this. 
	//I include it here for the sake of initializing decks with new cards. 
	public void AddCard(Card card, int index = 0){
		index = ActualPosition(index);
		cards.Insert(index, card);
		Render();
	}
		
	private protected int ActualPosition(int index){
		if(index < 0) index = index + cards.Count;
		if(index < 0 || index > cards.Count) Debug.LogError("Position " + index + " was not within Deck " + gameObject);
		return index;
	}

	private void Render(){
		Render(this.preset);
	}

	private void Render(DeckPreset preset){
		preset.Apply(cards, this);
	}

	public IEnumerator GetEnumerator(){
		return cards.GetEnumerator();
	}


}
