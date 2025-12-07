using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class CardGame : MonoBehaviour
{
	//A CardGame as a container for Decks is underlied by a Dictionary, where the Decks (value) can be accessed through their string deck names (key). 
	private protected Dictionary<string, Deck> playerDecks;
	private protected Dictionary<string, Deck> npcDecks;
	private protected Dictionary<string, Deck> sharedDecks;

	[SerializeField] Inventory playerInv;
	[SerializeField] private protected Deck botStartingCards;
	[SerializeField] private protected Deck extraCards;
	[SerializeField] private protected GameInput npcInput;

	private protected State currentState;

	private protected User currentUser;

	public static CardGame main;

	private protected void OnEnable(){
		playerInv = KeyUtil.main.PlayerInventory();
		playerDecks = new Dictionary<string, Deck>();
		npcDecks = new Dictionary<string, Deck>();
		sharedDecks = new Dictionary<string, Deck>();

		AddDecks(playerDecks, playerInv.GetReapedCards(), playerInv.GetDeck(), PlayerDeckList());
		AddDecks(npcDecks, GetNpcCards(), botStartingCards, NpcDeckList());
		AddDecksShared(sharedDecks);
		currentState = StartState();
		Debug.LogWarning("Initial State Set Up: " + currentState);
		if(main == null) main = this;
		else {
			Debug.LogWarning("Two Card Games attempting to run at the same time! Temporarily deactivating this CardGame (" + this + " on GameObject " + gameObject);
			enabled = false;
		}
	}

	private protected abstract Deck[] PlayerDeckList();

	private protected abstract Deck[] NpcDeckList();

	//TODO: Check if the card game was won or lost, and then set main to null to end the game. 
	private protected void Update(){
		Debug.Log("Player: " + currentUser + ", State: " + currentState);
		State temp = currentState.Do(CurrentInput());
		if(temp == null){
			StartNewTurn();
		}
		else if(temp is WinState) {
			Debug.LogError("End of game reached!");
			ReclaimPlayerCards();
			Debug.Log("Exiting to Scene: " + EnterFight.ReturningScene());
			SceneManager.LoadScene(EnterFight.ReturningScene().ToString());
		}
		else if(temp != currentState) currentState = temp;
	}

	private protected void ReclaimPlayerCards(){
		//Consolidate all Cards from all useful Decks into one Deck. This is because transferring a specific Card requires knowing the Deck that contains the Card. 
		foreach(Deck deck in playerDecks.Values) Deck.MoveCards(deck, extraCards);
		foreach(Deck deck in npcDecks.Values) Deck.MoveCards(deck, extraCards);
		foreach(Deck deck in sharedDecks.Values) Deck.MoveCards(deck, extraCards);

		Debug.Log("=D");
		Debug.Log(playerInv);
		Debug.Log(playerInv.GetReapedCards());
		foreach(Card c in playerInv.GetReapedCards()){
			Deck.MoveCard(extraCards, c, playerInv.GetDeck());
		}
	}

	abstract private protected List<Card> GetNpcCards();

	//Switches whose turn it is and sets up the starting phase of a turn. 
	private protected void StartNewTurn(){
		if(currentUser == User.player) currentUser = User.npc;
		else if(currentUser == User.npc) currentUser = User.player;
		else Debug.LogError("Unknown participant in game: " + currentUser); 
		currentState = StartState();
	}

	public GameInput CurrentInput(){
		if(currentUser == User.player) return UserInput.main;
		if(currentUser == User.npc) return npcInput;
		Debug.LogError("Could not get input because there was an unexpected user! Returning null. ");
		return null;
	}

	//This method is called in Awake(), and should be used to add the player-specific Decks necessary for each game. 
	abstract private protected void AddDecks(Dictionary<string, Deck> decks, List<Card> startingCards, Deck fromDeck, Deck[] deckList);

	//This method is called in Awake(), and should be used to add the Decks necessary for each game. 
	abstract private protected void AddDecksShared(Dictionary<string, Deck> decks);

	//Gets the state in which a player's turn begins. 
	abstract private protected State StartState();


	//Returns true if the string passed is a valid key representing a Deck. 
	//Does not detect whether the Deck the string refers to has been properly initialized/is not null. 
	public bool DeckExists(string deckName) {
		return playerDecks.ContainsKey(deckName);
	}

	//Gets a reference to a valid Deck, based on a deckName string.
	//Prints a warning message and some debugging information if deckName was not known to correspond to a Deck, returning null. 
	//Does not detect whether the Deck the string refers to has been properly initialized/is not null. 
	public Deck GetDeck(string deckName, User who) {
		if (!DeckExists(deckName))
		{
			string message = "Attempting to access invalid private deck " + deckName + ", which does not exist!\n";
			message += ValidDeckNames();
			Debug.LogWarning(message);
			return null;
		}
		else if (who == User.player) return playerDecks[deckName];
		return npcDecks[deckName];
	}

	public Deck GetDeck(string deckName){
		if(sharedDecks.ContainsKey(deckName)) return sharedDecks[deckName];
		return GetDeck(deckName, currentUser);
	}

	//Returns a string containing the valid deck names, separated by newlines, and with a descriptive one-line header. Mostly used for debugging purposes. 
	public string ValidDeckNames(){
		string results = "Valid Private Deck Names: ";
		foreach(string deckName in playerDecks.Keys) {
			results += "\n  " + deckName;
		}
		results += "- Note that this list only includes decks specific to the players. Decks that both players can access are not listed.";
		return results;
	}

	public static void SetCardGame(CardGame c){
		main = c;
	}

	public enum User
	{
		player,
		npc
	}

	public abstract class State{
		//Returns the next state. If it is equal to the current state, do nothing. If it is null, end the turn. 
		abstract public State Do(GameInput input);
	}
}
