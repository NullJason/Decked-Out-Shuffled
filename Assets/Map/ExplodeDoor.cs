using UnityEngine;

public class ExplodeDoor : EventAction
{
    [SerializeField] GameObject ExplosionEffect;
    [SerializeField] Animator animator;
    public override void DoEventAction()
    {
        Instantiate(ExplosionEffect,transform);
        animator.SetTrigger("OpenDoor");
        if(TryGetComponent<Collider2D>(out Collider2D col))
        {
            col.isTrigger = true;
        }
    }
}
