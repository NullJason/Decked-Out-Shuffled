using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

//  Opens a UI panel for selecting the cards for a fight, 
//  begins an Inventory card selection, 
//  displays a card's description when relevant, 
//  displays how many cards are selected and how many are necessary, 
//  checks whether the correct number of cards are selected, 
//  waits for the user to press a button while the right number of cards are selected, 
//  and reaps the cards selected by the user, allowing them to be used by another script.
//
//  TODO: Make it so that certain fields that are reused a lot are stored statically. 
public class EnterFight : Interaction
{
	//Represents the panel on which the ui for selecting cards is. 
	[SerializeField] private protected GameObject uiPanel;

	//Represents the textbox on which the card description should be printed. 
	[SerializeField] private protected TMP_Text description;

	//Represents the textbox for displaying number of cards required for this fight. 
	[SerializeField] private protected TMP_Text count;

	//How many cards are required for this fight. An exact number, not a range or a minimum. 
	[SerializeField] private protected int howManyCardsToChoose;

	//The Inventory responsible for selecting cards. 
	[SerializeField] private protected Inventory inv;

	//The card game to be started. 
	[SerializeField] private protected string sceneName;


	bool ready = false;

	private protected void Awake(){
		uiPanel.SetActive(false);
	}

	private protected override void StuffToDo(){
		uiPanel.SetActive(true);
		inv.StartSelection();
		ready = true;
	}

	private protected void Update(){
		if(ready){
			count.text = inv.GetCount() + "/" + howManyCardsToChoose;
			if(inv.GetCount() == howManyCardsToChoose){
				count.text += "\nPress space to begin!";
				if(Input.GetKeyDown("space") || Input.GetKeyDown("enter")){
					inv.ReapCards(); 
					//TODO!!
					

					SceneManager.LoadScene(sceneName);
		//			Debug.LogError("Here, a new scene would begin for a card battle, but that functionality has not yet been added!");
				}
			}
		}
	}
}
