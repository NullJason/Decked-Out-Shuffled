using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// should be place on Environment root
/// Add LevelData entries and assign the level container GameObjects and their Paths children.
/// Use the context menu "Auto Populate Paths" on each LevelData to auto-fill paths from a child folder named "Paths".
/// This hasn't been tested extensively, todo.
/// </summary>
public class LevelsManager : MonoBehaviour
{
    public static LevelsManager Instance { get; private set; }

    [Serializable]
    public class PathEntry
    {
        public string name;      // (e.g. "EntryA", "Path1", "MainDoor")
        public Transform target; // transform to teleport player to
    }

    [Serializable]
    public class LevelData
    {
        public string id;                // unique id for this level (used by SceneTransition)
        public GameObject container;     // the root GameObject that contains this level's objects
        public List<PathEntry> paths = new List<PathEntry>();

        // Helper used by context menu to populate paths from a child named "Paths"
        public void AutoPopulate()
        {
            paths.Clear();
            if (container == null) return;
            var trans = container.transform.Find("Paths");
            if (trans == null)
            {
                // recursive search for "Paths" if not directly a child
                trans = FindChildRecursive(container.transform, "Paths");
            }
            if (trans == null) return;

            for (int i = 0; i < trans.childCount; i++)
            {
                var child = trans.GetChild(i);
                var p = new PathEntry { name = child.name, target = child };
                paths.Add(p);
            }
        }

        private Transform FindChildRecursive(Transform parent, string name)
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
    }

    [Tooltip("List your levels here. Give each a unique id and assign its container GameObject.")]
    public List<LevelData> levels = new List<LevelData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>Get LevelData by id (returns null if not found)</summary>
    public LevelData GetLevel(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i] != null && levels[i].id == id) return levels[i];
        }
        return null;
    }

    /// <summary>
    /// Activate the level with the given id and deactivate sibling containers (if any).
    /// Returns true on success.
    /// </summary>
    public bool ActivateLevelById(string id)
    {
        var lvl = GetLevel(id);
        if (lvl == null)
        {
            Debug.LogWarning($"LevelsManager: No level with id '{id}' registered.");
            return false;
        }

        if (lvl.container == null)
        {
            Debug.LogWarning($"LevelsManager: Level '{id}' has no container set.");
            return false;
        }

        Transform parent = lvl.container.transform.parent;
        if (parent != null)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(child == lvl.container);
            }
        }
        else
        {
            lvl.container.SetActive(true);
        }

        return true;
    }

    /// <summary>Return the Transform for the named path in the given level, or null.</summary>
    public Transform GetPathTransform(string levelId, string pathName)
    {
        var lvl = GetLevel(levelId);
        if (lvl == null) return null;

        if (lvl.paths == null || lvl.paths.Count == 0) return null;
        for (int i = 0; i < lvl.paths.Count; i++)
        {
            var p = lvl.paths[i];
            if (p == null) continue;
            if (p.name == pathName) return p.target;
        }
        return null;
    }

#if UNITY_EDITOR
    // Editor convenience: auto-populate all registered levels' paths.
    [ContextMenu("Auto Populate All Paths")]
    private void AutoPopulateAll()
    {
        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i] != null)
            {
                levels[i].AutoPopulate();
            }
        }
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("LevelsManager: Auto-populated paths for all levels.");
    }
#endif
}
