using UnityEngine;

public class TestingStatePlayerPos : MonoBehaviour
{
    public bool isTesting = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(!isTesting) FindFirstObjectByType<Player>().transform.position = transform.position;
    }
}
