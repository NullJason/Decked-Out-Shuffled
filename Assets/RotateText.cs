using UnityEngine;

public class RotateText : MonoBehaviour
{
    private const float RotationSpeed = 45f;

    void Update()
    {
        transform.Rotate(Vector3.forward, RotationSpeed * Time.deltaTime);
    }
}
