using UnityEngine;

/*
 * A specialized Interactable that will activate some GameObject as a display when this DisplayedInteractable is the Interactable that an InteractAgent is closest to. 
 *
 */

public class DisplayedInteractable : Interactable
{
	[SerializeField] GameObject display;
//	private void Start() {
//		if(display == null) Debug.LogWarning("No display set for DisplayedInteractable " + name);
//	}
	

	public override void OnOver()
	{
		if(display != null) display.SetActive(true);
		else Debug.LogWarning("Missing display for DisplayedInteractable " + gameObject.name);
	}
	
	public override void OnNotOver()
	{
		if(display != null) display.SetActive(false);
		else Debug.LogWarning("Missing display for DisplayedInteractable " + gameObject.name);
	}
}
