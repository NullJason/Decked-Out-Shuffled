using UnityEngine;

public class WinState : CardGame.State
{
	bool who; //Represents whether the current player won or lost. 
	public WinState(bool who){
		this.who = who;
	}
	public override CardGame.State Do(){
		Debug.LogWarning("This method should not be called. A Win State should automatically be recognized as the end of a game, and should never actually have its Do() method called.");
		return this;
	}
	public bool whoWon(){
		return who;
	}
}
