using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneTransition
{
    // Public configuration / state
    public static float TransitionTime = 1f; // total time to wait AFTER new scene fully loads, before DoTransitionEnd
    public static float AnimSpeed = 1f; // multiplier applied to Animator.speed if Animator exists

    public static GameObject TransitionEffect;     

    // Ensure the hidden persistent behaviour exists and is initialized
    static SceneTransition()
    {
        SceneTransitionBehaviour.EnsureExists();
    }

    public static void AttachTransitionItems(GameObject transitionPrefab)
    {
        TransitionEffect = transitionPrefab;
    }
    /// <summary>
    /// overload
    /// loads level based only on path name/id. loads the first found. not recommended
    /// 
    /// specify the level by its id registered in LevelsManager and a path name (no tags).
    /// If not currently in the Environment scene this triggers the transition to load it and will
    /// perform activation/teleport after the Environment scene is loaded.
    /// </summary>
    public static void LoadEnvironmentLevel(string PathName)
    {
        LoadEnvironmentLevel(LevelsManager.Instance.GetLevelIDFromPathID(PathName), PathName);
    }
    /// <summary>
    /// specify the level by its id registered in LevelsManager and a path name (no tags).
    /// If not currently in the Environment scene this triggers the transition to load it and will
    /// perform activation/teleport after the Environment scene is loaded.
    /// </summary>
    public static void LoadEnvironmentLevel(string levelId, string entryName)
    {
        if (string.IsNullOrEmpty(levelId))
        {
            Debug.LogError("SceneTransition.LoadEnvironmentLevel: levelId is null/empty.");
            return;
        }

        // If already in Environment scene -> perform activation now and start animation-only transition.
        if (SceneManager.GetActiveScene().name == "Environment")
        {
            bool ok = ActivateLevelAndTeleportById(levelId, Player.Player_Transform, entryName);
            if (!ok) Debug.LogWarning($"SceneTransition: ActivateLevelById('{levelId}') failed while already in Environment.");
            StartTransition(); // play anim only
            return;
        }

        // Not in Environment: register one-time sceneLoaded handler and start transition load.
        Debug.Log($"SceneTransition: Loading 'Environment' and will activate level '{levelId}' entry '{entryName}' once loaded.");

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Environment") return;

            
            // Try to find LevelsManager in the loaded scene. It may take a frame to exist; wait briefly.
            LevelsManager lm = null;
            const int maxFramesToWait = 8;
            int frames = 0;
            while (lm == null && frames < maxFramesToWait)
            {
                lm = LevelsManager.Instance;
                if (lm != null) break;
                frames++;
                // give Unity a frame to finish Awake/Start on scene objects
                // Note: this runs inside sceneLoaded callback (synchronous), so we schedule a short delayed call:
                // We can't yield here; instead try a quick fallback: find object by type
                var found = GameObject.FindFirstObjectByType <LevelsManager>();
                if (found != null) { lm = found; break; }
            }

            if (lm == null)
            {
                lm = GameObject.FindFirstObjectByType <LevelsManager>();
            }

            if (lm == null)
            {
                Debug.LogWarning("SceneTransition: LevelsManager not found in loaded Environment scene. Cannot activate level by id.");
            }
            else
            {
                bool ok = lm.ActivateLevelById(levelId);
                if (!ok)
                {
                    Debug.LogWarning($"SceneTransition: LevelsManager failed to activate level '{levelId}'.");
                }
                else
                {
                    // get path transform and teleport player if available
                    var pathT = lm.GetPathTransform(levelId, entryName);
                    if (pathT != null)
                    {
                        Transform player = Player.Player_Transform;
                        if (player == null)
                        {
                            var pgo = GameObject.FindFirstObjectByType<Player>();
                            if (pgo != null) player = pgo.transform;
                        }
                        if (player != null)
                        {
                            player.position = pathT.position;
                            var rb2d = player.GetComponent<Rigidbody2D>();
                            if (rb2d != null) rb2d.linearVelocity = Vector2.zero;
                            else
                            {
                                var rb = player.GetComponent<Rigidbody>();
                                if (rb != null) rb.linearVelocity = Vector3.zero;
                            }
                        }
                        else
                        {
                            Debug.LogWarning("SceneTransition: Player transform null and no GameObject 'Player' found.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"SceneTransition: Path '{entryName}' not found for level '{levelId}'.");
                    }
                }
            }            
            
            SceneManager.sceneLoaded -= OnSceneLoaded;
            
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        StartTransition();
    
    }
    
    /// <summary>Helper used when already in Environment: activate level and teleport via LevelsManager instance.</summary>
    private static bool ActivateLevelAndTeleportById(string levelId, Transform player, string entryName)
    {
        var lm = LevelsManager.Instance ?? GameObject.FindFirstObjectByType<LevelsManager>();
        if (lm == null)
        {
            Debug.LogWarning("SceneTransition: LevelsManager not found in current scene.");
            return false;
        }

        if (!lm.ActivateLevelById(levelId))
            return false;

        var pathT = lm.GetPathTransform(levelId, entryName);
        if (pathT == null)
        {
            Debug.LogWarning($"SceneTransition: Path '{entryName}' not found for level '{levelId}'.");
            return true; // activation succeeded; only teleport couldn't be done
        }

        Transform playerTransform = player;
        if (playerTransform == null)
        {
            var pgo = GameObject.FindFirstObjectByType<Player>();
            if (pgo != null) playerTransform = pgo.transform;
        }

        if (playerTransform == null)
        {
            Debug.LogWarning("SceneTransition: Player transform null and no GameObject 'Player' found.");
            return true;
        }

        playerTransform.position = pathT.position;
        var rb2d = playerTransform.GetComponent<Rigidbody2D>();
        if (rb2d != null) rb2d.linearVelocity = Vector2.zero;
        else
        {
            var rb = playerTransform.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
        }

        return true;
    }

    public static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            var f = FindChildRecursive(c, name);
            if (f != null) return f;
        }
        return null;
    }
    public static void StartTransition()
    {
        TransitionEffect.SetActive(true);
    }
    
    public static void StartTransition(string scene)
    {
        StartTransition();
        SceneManager.LoadScene(scene);
    }

}

