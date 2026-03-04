using UnityEngine;

public class ExplodeDoor : EventAction
{
    [SerializeField] GameObject ExplosionEffect;
    [SerializeField] GameObject Door;
    [SerializeField] Animator animator;
    public override void DoEventAction()
    {
        Instantiate(ExplosionEffect,transform);
        if(animator != null)animator.SetTrigger("OpenDoor");
        if(Door.TryGetComponent<Collider2D>(out Collider2D col))
        {
            col.isTrigger = true;
        }
    }
}
