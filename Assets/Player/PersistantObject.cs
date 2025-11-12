using UnityEngine;

public class PersistantObject : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(this);
    }
    void Start()
    {
        DontDestroyOnLoad(this);
    }
    void OnEnable()
    {
        DontDestroyOnLoad(this);
    }
}
