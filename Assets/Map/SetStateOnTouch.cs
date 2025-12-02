using Unity.VisualScripting;
using UnityEngine;

public class SetStateOnTouch : MonoBehaviour
{
    [SerializeField] private GameObject StateObject;
    [SerializeField] private Collider2D DetectObject;
    [SerializeField] private bool NewState = false;
    [Header("for multiple activations")]
    [SerializeField] private bool IsToggle = false;
    [SerializeField] private int ToggleLimit = 1;
    [SerializeField] private float ToggleCoolDown = 2; // not used rn.

    private int toggleCount = 0;
    void OnEnable()
    {
        if(TryGetComponent<Collider2D>(out Collider2D col))
        {
            col.isTrigger = true;
        }
        else
        {
            transform.AddComponent<BoxCollider2D>().isTrigger = true;
        }
        if(DetectObject == null) DetectObject = FindFirstObjectByType<Player>().GetComponent<Collider2D>();
    }
    private void OnTriggerEnter2D(Collider2D other) {
        if (other.Equals(DetectObject))
        {
            if(StateObject.activeSelf != NewState) {
                StateObject.SetActive(NewState);
                if(IsToggle && toggleCount < ToggleLimit ) { NewState = !NewState; toggleCount++; }
            }
        }
    }
}
