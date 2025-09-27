using UnityEngine;
using System.Collections.Generic;

/* An InteractAgent detects when it is near an Interactable, and conveys information to that Interactable. 
 * Specifically, it lets the Interactable know whether that Interactable is the closest within range, and it may also triggers interactions with that Interactable. 
 */
public class InteractAgent : MonoBehaviour
{
	[SerializeField]
	private static InteractAgent defaultInteractAgent;

	//A set of the known Interactables that will be able to interact with this InteractAgent. 
	private HashSet<Interactable> targets;

	private double maxInteractionDistance = 5;
	private void Awake(){
		targets = new HashSet<Interactable>();
		if(defaultInteractAgent == null) defaultInteractAgent = this;
	}

	//Adds an Interactable to the set of all Interactables that this InteractAgent can interact with. 
	public void Add(Interactable toAdd){
		targets.Add(toAdd);
	}

	public static void AddToDefault(Interactable toAdd){
		defaultInteractAgent.Add(toAdd);
	}

	private void Update(){
		foreach(Interactable i in targets){
			i.OnNotOver();
		}
		Interactable nearest = FindNearest();
		if(nearest != null){
			if(CheckPlayerInteraction()) nearest.Interact();
			nearest.OnOver();
		}
	}

	//Conditionally returns the nearest Interactable within the set of Interactables that this InteractAgent can interact with. 
	//If the nearest Interactable is farther than the maximum interaction distance, returns null. 
	private Interactable FindNearest(){
		Interactable nearest = null;
		double farthestDistance = 0;
		foreach(Interactable target in targets){
			double targetDistance = (transform.position - target.gameObject.transform.position).sqrMagnitude;
			if(targetDistance < farthestDistance || nearest == null){
				nearest = target;
				farthestDistance = targetDistance;
			}
		}
		if(farthestDistance > maxInteractionDistance) return null;
		return nearest;
	}

	private protected bool CheckPlayerInteraction(){
		return Input.GetKeyDown(KeyCode.Space);
	}
}
