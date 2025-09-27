using UnityEngine;
/*
 * An Interaction that sends some text using Debug.Log(string).
 */
public class LogInteraction : Interaction
{
	[SerializeField]
	private protected string message;
	private protected override void StuffToDo()
	{
		Debug.Log(message);
	}
}
