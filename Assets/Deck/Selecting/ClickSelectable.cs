using UnityEngine;
using UnityEngine.UI;

//Something a player can click on to select it. 
//See also: class UserInput, the class in charge of detecting the player's most recent selections. 

public class ClickSelectable : Selectable
{
	[SerializeField]
	private Button button;

	[SerializeField]
	private CanvasRenderer display;

	private void Awake(){
		//Unnecessary. Already added on the prefab directly. 
		//button.onClick.AddListener(Select);

		Unhighlight();
	}

	public override void Highlight(){
		display.SetAlpha(0.5f);
		button.enabled = true;
	}

	public override void Unhighlight(){
		display.SetAlpha(0);
		button.enabled = false;
	}

	public override void Select(){
		Debug.Log("Card was selected!");
		CardGame.main.CurrentInput().Try(this);
	}
}
