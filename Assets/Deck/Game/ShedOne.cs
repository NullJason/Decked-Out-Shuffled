using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShedOne : CardGame
{
	[SerializeField] Deck discard;
	[SerializeField] Deck playerDraw;
	[SerializeField] Deck playerHand;
	[SerializeField] Deck npcDraw;
	[SerializeField] Deck npcHand;
	private protected override Deck[] PlayerDeckList(){
		return new Deck[] {playerDraw, playerHand};
	}
	private protected override Deck[] NpcDeckList(){
		return new Deck[] {npcDraw, npcHand};
	}

	private protected override void AddDecks(Dictionary<string, Deck> decks, List<Card> startingCards, Deck[] deckList){
		//Create appropriate Decks. 
		decks.Add("Draw pile", deckList[0]);
		decks.Add("Hand", deckList[1]);

		//Shuffle the starting cards. 
		//TODO!!
		Debug.LogWarning("Shuffling cards has not yet been implemented!");

		//Get top five cards for hand. 
		for(int i = 0; i < 5; i++) {
			Card c = startingCards[startingCards.Count - 1];
			startingCards.RemoveAt(startingCards.Count - 1);
			decks["Hand"].AddCard(c);
		}

		//Get the rest of the cards for the draw pile. 
		foreach(Card c in startingCards) {
			decks["Draw pile"].AddCard(c);
		}

	}

	override private protected void AddDecksShared(Dictionary<string, Deck> decks){
		decks.Add("Discard", discard);
		Deck.MoveCard(extraCards, extraCards.GetCardAtIndex(0), GetDeck("Discard"));
	}

	override private protected State StartState(){
		Debug.LogError("Good!");
		return new PlayerCanPlayCards();
	}

	override private protected List<Card> GetNpcCards(){
		List<Card> results = new List<Card>();
		foreach(Card c in botStartingCards) results.Add(c);
		return results;
	}



	//The state where the player chooses which card to play. 
	//Waits for player input, and then moves the selected card from the hand to the top of the discard pile. 
	private protected class ChooseWhichCardToPlayState : State{
		
		override public State Do(){
			Selectable s = UserInput.main.GetNext();
			if(s != null) {
				if(!(s is Card)) Debug.LogError("Somehow got a non-Card where a Card was expected!");
				Deck.MoveCard(CardGame.main.GetDeck("Hand"), (Card) s, CardGame.main.GetDeck("Discard"));
				if(CardGame.main.GetDeck("Hand").Count() <= 0) return new WinState(true); //Win the game if you have no cards left after moving your card. 
				return null; //Otherwise, return null (ends turn immediately). 
			}
			return this;
		}
	}

	private protected class PlayerCanPlayCards : State{
		override public State Do(){

			HashSet<Selectable> cards = new HashSet<Selectable>();
			Card discard = CardGame.main.GetDeck("Discard").GetCardAtIndex(0); //The card at the top of the discard pile. 
			foreach(Card card in CardGame.main.GetDeck("Hand")) { //Find playable cards in the hand. 
				if(CardsCompatible(discard, card)){
					cards.Add(card);
				}
			}
			if(cards.Count <= 0){ //If no playable cards were found in the hand, add the top card of the draw pile to the player's options, then start a new DrawCard State. 
				if(CardGame.main.GetDeck("Draw pile").Count() <= 0 ) return new WinState(false); //Lose the game if there are no cards you can play and your draw pile is empty. 
				cards.Add(CardGame.main.GetDeck("Draw pile").GetCardAtIndex(0));
				return new DrawCardState();
			}

			return new ChooseWhichCardToPlayState(); //If playable cards were found, transition to a stage where the player chooses which one to play. Note that the input system has already been set up with which cards to play. 

		}

		private protected bool CardsCompatible(Card c1, Card c2){
			if(c1.HasAttribute("Red") && c2.HasAttribute("Red")) return true;
			if(c1.HasAttribute("Green") && c2.HasAttribute("Green")) return true;
			if(c1.HasAttribute("Blue") && c2.HasAttribute("Blue")) return true;
			if(c1.HasAttribute("Yellow") && c2.HasAttribute("Yellow")) return true;
			if(c1.HasAttribute("One") && c2.HasAttribute("One")) return true;
			if(c1.HasAttribute("Two") && c2.HasAttribute("Two")) return true;
			if(c1.HasAttribute("Three") && c2.HasAttribute("Three")) return true;
			if(c1.HasAttribute("Four") && c2.HasAttribute("Four")) return true;
			if(c1.HasAttribute("Five") && c2.HasAttribute("Five")) return true;
			if(c1.HasAttribute("Six") && c2.HasAttribute("Six")) return true;
			if(c1.HasAttribute("Seven") && c2.HasAttribute("Seven")) return true;
			if(c1.HasAttribute("Eight") && c2.HasAttribute("Eight")) return true;
			if(c1.HasAttribute("Nine ") && c2.HasAttribute("Nine")) return true;
			if(c1.HasAttribute("Ten") && c2.HasAttribute("Ten")) return true;
			return false;
		}
	}

	private protected class DrawCardState : State{
		override public State Do(){
			Selectable s = UserInput.main.GetNext();
			if(s != null) {
				if(!(s is Card)) Debug.LogError("Somehow got a non-Card where a Card was expected!");
				Deck.MoveCard(CardGame.main.GetDeck("Draw"), (Card) s, CardGame.main.GetDeck("Hand")); //Move card from draw pile to hand (i.e. draw a card). 
				return null;
			}
			return this;
			
		}
	}
}

