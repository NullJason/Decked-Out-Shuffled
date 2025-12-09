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
    
    public GameObject TransitionCanvas;    
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(_instance.gameObject);
            _instance = null;
        }

        EnsureExists();
        SceneTransition.AttachTransitionItems(TransitionCanvas);
    }

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

}
