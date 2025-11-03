using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//When the player opens a UI to select what cards they want to use in a fight, the Inventory class creates a UI for swapping with toggles for each card. 
//  (Actually, the UI panel pre-exists. The Inventory class really just creates the Toggles.)
//When the player confirms their selection, ReapCards() is called, and the cards are returned based on whether they were toggled. The toggles themselves are destroyed afterwards. 

public class Inventory : MonoBehaviour
{
    //Represents whether cards are being selected through toggles. Some actions may behave unexpectedly depending on whether this is true or false. 
    private protected bool selectMode = false;

    [SerializeField] private protected Transform ui;
    [SerializeField] private protected GameObject uiPanel;
    //what distance separates two panels on the display.
    [SerializeField] private protected float panelDeltaY = -50;
    private protected List<Card> cards;
    private protected List<Toggle> toggles;
    private void Awake()
    {
        //TODO: Add save system... later. 
        cards = new List<Card>();
    }

    public void AddCard(Card c)
    {
	if(selectMode) Debug.LogError("Cannot add new cards to inventory when select mode is already active!");
        cards.Add(c);
    }
    public void PrintCards(){
	foreach(Card c in cards) Debug.Log(c);
    }

    //Should be called when the player wants to confirm their card selections. 
    //By getting the toggles, checks which cards were selected, returning them. 
    //Destroys the old toggles. 
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
        return results;
    }

    //Returns the number of cards toggled on. 
    //Use for, say, checking whether the player has selected too many or too few cards. 
    public int GetCount()
    {
	if(!selectMode) Debug.LogError("Cannot get number of cards selected when select mode is not active!");
        int count = 0;
        foreach (Toggle toggle in toggles)
        {
            if(toggle.isOn) count++;
        }
        return count;
    }

    //Creates the new toggles on the UI. 
    public void StartSelection()
    {
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

}
