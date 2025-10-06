using System.Collections.Generic;
using UnityEngine;

public class TestCards : MonoBehaviour
{
	[SerializeField] Selectable[] cardsArray1;
	[SerializeField] Selectable[] cardsArray2;
	[SerializeField] Dialogue dialogue;
	HashSet<Selectable> cards1;
	HashSet<Selectable> cards2;
	int stage = 0;
	Selectable results1;
	Selectable results2;
	Selectable results3;
	void Awake(){
		cards1 = new HashSet<Selectable>();
		cards2 = new HashSet<Selectable>();
		foreach(Selectable c in cardsArray1){
			cards1.Add(c);
		}
		foreach(Selectable c in cardsArray2){
			cards2.Add(c);
		}
	}

	void Update(){
		TestSelectCards();
	}

	void TestSelectCards(){
		if(stage == 0) {
			UserInput.main.WaitForNewInput(cards1);
			dialogue.QueueDialogue("Select a card from your deck.", KeyCode.Return);
			dialogue.PlayNext();
			stage = 1;
		}
		else if(stage == 1) {
			results1 = UserInput.main.GetNext();
			if(results1 != null) stage = 2;
		}
		else if(stage == 2) {
			dialogue.QueueDialogue("Card " + results1.gameObject + " selected! Now, select a card from the table.", KeyCode.Return);
			dialogue.PlayNext();
			cards1.Remove(results1);
			UserInput.main.WaitForNewInput(cards2);
			stage = 3;
		}
		else if(stage == 3) {
			results2 = UserInput.main.GetNext();
			if(results2 != null) stage = 4;
		}
		else if(stage == 4) {
			dialogue.SetDialogue("Card " + results2.gameObject + " selected! Select another card from your hand.", KeyCode.Return);
			dialogue.PlayNext();
			UserInput.main.WaitForNewInput(cards1);
			stage = 5;
		}
		else if(stage == 5) {
			results3 = UserInput.main.GetNext();
			if(results3 != null) stage = 6;
		}
		else if(stage == 6) {
			dialogue.SetDialogue("Card " + results3.gameObject + " selected! The three cards have been destroyed. Thank you for playing!", KeyCode.Return);
			dialogue.PlayNext();
			Destroy(results1.gameObject);
			Destroy(results2.gameObject);
			Destroy(results3.gameObject);
			UserInput.main.Clear();
			stage = -1;
		}
	}
}
