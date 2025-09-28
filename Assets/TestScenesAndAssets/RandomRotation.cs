using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class NewMonoBehaviourScript : MonoBehaviour
{
    private Rigidbody rb;
    Outline ol;
    void Start()
    {
        if (rb == null)
        {
            rb = transform.AddComponent<Rigidbody>();
            rb.useGravity = false;
        }

        ol = gameObject.GetComponent<Outline>();
        ol.enabled = true;
        ol.OutlineColor = Color.white;
        ol.OutlineWidth = 7.0f;
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(.1f, 0, 1);
    
    }
}