using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Hidden MonoBehaviour used to run coroutines and keep the transition objects alive across scenes.
/// Created once and set to DontDestroyOnLoad automatically.
/// </summary>
public class SceneTransitionBehaviour : MonoBehaviour
{
    private static SceneTransitionBehaviour _instance;
    private static readonly string ManagerName = "SceneTransitionManager";
    
    public Canvas TransitionCanvas;   
    public Transform AnimatedSprite; 
    private Animator TransitionAnimator;   
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(_instance.gameObject);
            _instance = null;
        }

        EnsureExists();
        SceneTransition.AttachTransitionItems(_instance.TransitionCanvas, _instance.AnimatedSprite, _instance.TransitionAnimator);
    }

    // Ensure there's exactly one instance in the game 
    public static SceneTransitionBehaviour EnsureExists()
    {
        if (_instance != null) return _instance;

        var existing = GameObject.Find(ManagerName);
        if (existing != null)
        {
            _instance = existing.GetComponent<SceneTransitionBehaviour>();
            DontDestroyOnLoad(existing);
            if (_instance != null) return _instance;
        }

        var go = new GameObject(ManagerName);
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<SceneTransitionBehaviour>();
        
        return _instance;
    }

    // Public entry to start the coroutine from the static class
    public void StartTransitionCoroutine(string sceneName,
        float transitionTime, Animator animator, Canvas TransitionCanvas)
    {
        Debug.Log("startingg");
        StartCoroutine(TransitionRoutine(sceneName, transitionTime, animator, TransitionCanvas));
    }

    private IEnumerator TransitionRoutine(string sceneName,
        float transitionTime, Animator animator, Canvas TransitionCanvas)
    {
        bool willLoadScene = !string.IsNullOrEmpty(sceneName);

        // Trigger the start of the animation immediately
        if (animator != null)
        {
            animator.SetTrigger("DoTransitionStart");
            Debug.Log("SceneTransition: Animator triggered 'DoTransitionStart'.");
        }
        
        else
        {
            Debug.Log("SceneTransition: No Animator/Animation found. Can't play transition animation.");
        }

        if (!willLoadScene)
        {
            Debug.Log("No Scene passed, playing anim only.");
            // Just wait TransitionTime then trigger end
            float elapsed = 0f;
            while (elapsed < transitionTime)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (animator != null) animator.SetTrigger("DoTransitionEnd");

            yield break;
        }

        // --- SCENE LOADING PATH ---
        // Start loading the scene in background but DO NOT activate yet
        Debug.Log("Attempting to Load Scene");
        var async = SceneManager.LoadSceneAsync(sceneName);
        if (async == null)
        {
            Debug.LogError($"SceneTransition: Failed to start loading scene '{sceneName}'.");
            yield break;
        }

        async.allowSceneActivation = false;

        // Wait until background load reaches 0.9 (Unity convention: progress goes to 0.9 and waits for activation).
        while (async.progress < 0.9f)
        {
            yield return null;
        }

        // At this point the scene is loaded in memory. Allow activation so Unity will switch to the new scene.
        async.allowSceneActivation = true;

        // Wait until activation completes
        while (!async.isDone)
        {
            yield return null;
        }

        // Give Unity one frame to run Awake/OnEnable/Start on new scene objects.
        // Also wait for end of frame to ensure render/graphics objects are created.
        yield return null;
        yield return new WaitForEndOfFrame();

        // Now the new scene should be fully initialized. Wait the configured TransitionTime,
        // then trigger DoTransitionEnd so end-of-transition animation plays on top of the new scene.
        float waited = 0f;
        while (waited < transitionTime)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        if (animator != null)
        {
            animator.SetTrigger("DoTransitionEnd");
            Debug.Log("SceneTransition: Animator triggered 'DoTransitionEnd' after scene load + delay.");
        }
        else TransitionCanvas.gameObject.SetActive(false);

        Debug.Log("completed transition");

        yield break;
    }
}
