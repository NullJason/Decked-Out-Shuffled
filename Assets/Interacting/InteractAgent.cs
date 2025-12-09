using UnityEngine;
using System.Collections.Generic;

/* An InteractAgent detects when it is near an Interactable, and conveys information to that Interactable. 
 * Specifically, it lets the Interactable know whether that Interactable is the closest within range, and it may also triggers interactions with that Interactable. 
 */
public class InteractAgent : MonoBehaviour
{
	[SerializeField]
	private static InteractAgent defaultInteractAgent;
	[SerializeField] 
	public static KeyCode Interact_Key = KeyCode.Space;

	//A set of the known Interactables that will be able to interact with this InteractAgent. 
	private HashSet<Interactable> targets;

	private float maxInteractionDistance = 5;
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

	private void Update()
	{
		if (!Application.isPlaying) return; 
		if (targets == null || targets.Count == 0) return;
		
		Interactable nearest = FindNearest();
		if (nearest != null)
		{
			if (CheckPlayerInteraction())
				nearest.Interact();
			nearest.OnOver();
		}
		foreach (Interactable i in targets)
		{
			if (i == null || i.Equals(nearest)) continue;
			i.OnNotOver();
		}
		
		// foreach(Interactable i in targets){
		// 	i.OnNotOver();
		// }
		// Interactable nearest = FindNearest();
		// if(nearest != null){
		// 	if(CheckPlayerInteraction()) nearest.Interact();
		// 	nearest.OnOver();
		// }
	}

	//Conditionally returns the nearest Interactable within the set of Interactables that this InteractAgent can interact with. 
	//If the nearest Interactable is farther than the maximum interaction distance, returns null. 
	private Interactable FindNearest(){
		// Interactable nearest = null;
		// double farthestDistance = 0;
		// foreach(Interactable target in targets){
		// 	if(target == null || target.gameObject == null){Debug.LogWarning($"A Interactable added itself to targets but is null at check."); targets.Remove(target); }
		// 	double targetDistance = (transform.position - target.gameObject.transform.position).sqrMagnitude;
		// 	if(targetDistance < farthestDistance || nearest == null){
		// 		nearest = target;
		// 		farthestDistance = targetDistance;
		// 	}
		// }
		// if(farthestDistance > maxInteractionDistance || farthestDistance > nearest.InteractDist) return null;
		// return nearest;
		if (targets == null || targets.Count == 0) return null;

		
		float maxSqr = maxInteractionDistance * maxInteractionDistance;
		Interactable best = null;
		float bestSqr = 100000;
		Vector3 myPos = transform.position;

		foreach (var target in targets)
		{
			if (target == null || target.gameObject == null)
			{
				Debug.LogWarning($"A Interactable added itself to targets but is null at check."); 
				targets.Remove(target);;
				continue;
			}

			float distSqr = (myPos - target.gameObject.transform.position).sqrMagnitude;

			if (distSqr > maxSqr) continue;

			float interactDist = target.InteractDist;
			if (interactDist <= 0f) continue;
			float interactSqr = interactDist * interactDist;
			if (distSqr > interactSqr) continue; 

			if (distSqr < bestSqr)
			{
				bestSqr = distSqr;
				best = target;
			}
		}

		return best;
	}

	private protected bool CheckPlayerInteraction(){
		return Input.GetKeyDown(Interact_Key);
	}
}
