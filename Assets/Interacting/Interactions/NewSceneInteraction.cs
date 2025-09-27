using UnityEngine;
using UnityEngine.SceneManagement;

/* An Interaction that opens some scene by name. 
 */
public class NewSceneInteraction : Interaction
{

	[SerializeField] string sceneName;
	private protected override void StuffToDo()
	{
		SceneManager.LoadScene(sceneName);
	}
}
