using UnityEngine;

public class Util
{
	//A method that checks whether a Component is null. If it is, it will pring a warning message and attempt to find one on the GameObject passed using GetComponent. If no appropriate Component could be found, prints a message to Warning or Error, depending on whether the critical field is specified. 
	public static T NullCheck<T> (T component, GameObject g, bool critical = true) where T : Component {
		if(component != null) return component;

		Debug.LogWarning("No Component of type " + typeof(T) + " was given. Attempting to find a suitable " + typeof(T) + " on GameObject " + g + "... ");
		component = g.GetComponent<T>();

		if(component != null) return component;
		if(critical) Debug.LogError("No Component of type " + typeof(T) + " was given, and no suitable " + typeof(T) + " was found on GameObject " + g + "! ");
		else Debug.LogWarning("No Component of type " + typeof(T) + " was given, and no suitable " + typeof(T) + " was found on GameObject " + g + "! Null value will be used.");
		
		return null;
	}
	
}
