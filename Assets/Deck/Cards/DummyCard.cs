using UnityEngine;

public class DummyCard : Card
{
	public override void Effect(){
		Debug.Log("Card " + gameObject + "!");
	}
}
