using UnityEngine;

public class PermanentObject : MonoBehaviour
{
    // simply makes GO a Persistent object
    public static PermanentObject Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
