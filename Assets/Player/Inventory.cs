using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//When the player opens a UI to select what cards they want to use in a fight, the Inventory class creates a UI for swapping with toggles for each card. 
//  (Actually, the UI panel pre-exists. The Inventory class really just creates the Toggles.)
//When the player confirms their selection, ReapCards() is called, and the cards are returned based on whether they were toggled. The toggles themselves are destroyed afterwards. 
//Also responsible for storing the cards themselves. 

public class Inventory : MonoBehaviour
{
    //Represents whether cards are being selected through toggles. Some actions may behave unexpectedly depending on whether this is true or false. 
    private protected bool selectMode = false;

    [SerializeField]
    private protected Deck storage; //This was added after List<Card> cards was programmed into this and I don't want to deal with it right now. In theory, a Deck should be the only container a card can be stored in. In practice, an Inventory stores two copies of its content, one in a Deck and one in a List<Card>. 

    //The UI panel on which to place toggles. 
    [SerializeField] private protected Transform ui;

    //The GameObject or prefab representing a ui panel with a toggle. 
    //Must have a Toggle Component!
    [SerializeField] private protected GameObject uiPanel;

    //what distance separates two panels on the display.
    [SerializeField] private protected float panelDeltaY = -50;

    //The Cards that the user has acquired. 
    private protected List<Card> cards;

    //Stores any Toggles that are created. 
    private protected List<Toggle> toggles;


    //Represents the cards gotten in the most recent selection. 
    private protected List<Card> reapedCards;

    private void Awake()
    {
        //TODO: Add save system... later. 
        cards = new List<Card>();
    }

    public void AddCard(Card c, Deck fromDeck)
    {
	if(selectMode) Debug.LogError("Cannot add new cards to inventory when select mode is already active!");
        cards.Add(c);
	Deck.MoveCard(fromDeck, c, storage);
    }
    public void PrintCards(){
	foreach(Card c in cards) Debug.Log(c);
    }

    //Should be called when the player wants to confirm their card selections. 
    //By getting the toggles, checks which cards were selected, returning them. 
    //Destroys the old toggles. 
    //Also saves the reaped cards in reapedCards. 
    public List<Card> ReapCards()
    {
	if(!selectMode) Debug.LogError("Cannot get cards selected when select mode is not active!");
        List<Card> results = new List<Card>();
        for(int i = 0; i < toggles.Count; i++)
        {
            if (toggles[i].isOn) results.Add(cards[i]);
        }
	foreach(Toggle t in toggles){
		GameObject.Destroy(t.gameObject);
	}
	toggles = null;
	reapedCards = results;
	selectMode = false;

        return results;
    }

    public List<Card> GetReapedCards(){
	if(selectMode) Debug.LogError("Cannot get reaped cards, as cards are still being selected!");
	return reapedCards;
    }

    //Returns the number of cards toggled on. 
    //Use for, say, checking whether the player has selected too many or too few cards. 
    public int GetCount()
    {
	if(!selectMode) Debug.LogError("Cannot get number of cards selected when select mode is not active!");
        int count = 0;
	Debug.Log("Toggles: " + toggles);
        foreach (Toggle t in toggles)
        {
            if(t.isOn) count++;
        }
        return count;
    }

    //Creates the new toggles on the UI. 
    public void StartSelection()
    {
	Debug.Log("Card selection started!");
	if(selectMode) Debug.LogError("Cannot activate select mode when select mode is already active!");
	selectMode = true;
        toggles = new List<Toggle>();
        Vector3 pos = new Vector3(0, 0, 0);
        foreach (Card c in cards)
        {
            Transform temp = GameObject.Instantiate(uiPanel.transform, ui.transform.position + pos, new Quaternion(), ui); //TODO!!
            Toggle toggle = temp.GetComponent<Toggle>();
            toggles.Add(toggle);

	    pos.y += panelDeltaY;
        }
    }

    //TODO!! 
    //Used to get the card associated with a specific toggle so that you can, for example, get the description from the card. 
    //May throw exceptions if toggles and cards aren't set up right. 
    public Card GetCardFromToggle(Toggle t)
    {
	if(!selectMode) Debug.LogError("Cannot get card based on toggle when select mode is not active!");
	return(cards[toggles.IndexOf(t)]);
    }

    public Deck GetDeck(){
	    return storage;
    }
}
