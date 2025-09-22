using UnityEngine;

public class Player : MonoBehaviour
{
    public int MAX_INTERACT_DISTANCE = 10;
    public int MOVE_SPEED = 5;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 movement;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        Move();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            //TryInteract();
        }
    }

    void Move()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        movement = new Vector2(moveX, moveY).normalized;
        rb.linearVelocity = movement * MOVE_SPEED;

        // todo: animate if moving
        if (movement.magnitude > 0.01f)
        {
            // Animate("Walk");
        }
        else
        {
            // Animate("Idle");
        }
    }

    void Animate(string animTrack)
    {
        if (animator != null)
        {
            animator.Play(animTrack);
        }
    }

    //void TryInteract()
    //{
    //    // get closest interactable within max interact dist and tries to interact.

    //    Interactable[] interactables = FindObjectsOfType<Interactable>();
    //    Interactable closest = null;
    //    float closestDist = Mathf.Infinity;

    //    foreach (var obj in interactables)
    //    {
    //        float dist = Vector2.Distance(transform.position, obj.transform.position);
    //        if (dist < closestDist)
    //        {
    //            closestDist = dist;
    //            closest = obj;
    //        }
    //    }

    //    if (closest != null && closestDist <= MAX_INTERACT_DISTANCE)
    //    {
    //        Interact(closest);
    //    }
    //}

    //void Interact(Interactable interactable)
    //{
    //    if (interactable == null) return;
    //    interactable.OnInteract();
    //}
}
