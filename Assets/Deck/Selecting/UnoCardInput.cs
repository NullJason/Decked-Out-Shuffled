using UnityEngine;
using System.Collections.Generic;

public class UnoCardSelect : GameInput
{
	public override void Try(Selectable s){
		Debug.LogWarning("Manually selecting from outside the class is not supported for this selection!");
	}


	//When adding new Cards to a selectable, they are also added to the queue. 
	//This is because in this game, only valid actions are offerred. In ShedOne (or Uno), exactly which action is selected shouldn't matter too much, especially if we're only making a small demo. 
	//TODO: Make it add the action that's most useful (i.e. if most of your cards are either red or seven, it should play a red seven card if it has) instead of just choosing in the order passed!
	public override void WaitForNewInput(HashSet<Selectable> selectables){
    		Debug.Log("=D");
		base.WaitForNewInput(selectables);
		foreach(Selectable s in selectables) {
			TrySelect(s);
			Debug.Log(s);
		}
	}
}
