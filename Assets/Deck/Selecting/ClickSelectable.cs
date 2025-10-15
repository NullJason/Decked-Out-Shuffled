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
		button.onClick.AddListener(Select);
		Unhighlight();
	}

	private void Start(){
		UserInput.main.Add(this);
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
		UserInput.main.Try(this);
	}
}
