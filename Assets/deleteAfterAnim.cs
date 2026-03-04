using UnityEngine;

public class deleteAfterAnim : MonoBehaviour
{
    void Start()
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            Destroy(gameObject, stateInfo.length + 0.1f);
        }
        else
        {
            Debug.LogWarning("Animator component not found. Object not scheduled for destruction.");
        }
    }

}
