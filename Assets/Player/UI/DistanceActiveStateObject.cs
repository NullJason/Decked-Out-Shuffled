using UnityEngine;

public class DistanceActiveStateObject : MonoBehaviour
{
    [SerializeField] private int ActiveType = -1;
    [SerializeField] private float ActiveDistance = 6;
    private GameObject ActiveStateObject;
    private Transform DistanceObjectTransform;
    private Transform DistanceObjectTransform2;
    private bool HasSetState = false;
    private bool SingleTrigger;
    void Start()
    {
        if (ActiveType < 0) Debug.Log("active type not set (<0) 1=active inside distance, 2=active outside distance, 3=toggle once inside, 4 = toggle once outside");
    }
    public void SetNew(Transform objT, Transform objT2, GameObject objASO, bool SingleUse = false)
    {
        // Debug.Log($"setting new {objT.name},{objT2.name},{objASO.name},{SingleUse}");
        DistanceObjectTransform = objT;
        DistanceObjectTransform2 = objT2;
        ActiveStateObject = objASO;
        SingleTrigger = SingleUse;
        HasSetState = false;
    }
    void ResetState()
    {
        DistanceObjectTransform = null;
        DistanceObjectTransform2 = null;
        ActiveStateObject = null;
        SingleTrigger = false;
        HasSetState = false;
    }
    // Update is called once per frame
    void Update()
    {
        if (ActiveType < 0 || ActiveDistance < 0 || ActiveStateObject == null || DistanceObjectTransform == null) return;
        float Dist = (DistanceObjectTransform.position - DistanceObjectTransform2.position).magnitude;
        
        if (SingleTrigger && HasSetState) { ResetState(); return; }
        if (ActiveType == 4)
        {
            if (Dist < ActiveDistance) HasSetState = false;
            else if(!HasSetState && Dist > ActiveDistance)
            {
                HasSetState = true;
                ActiveStateObject.gameObject.SetActive(!ActiveStateObject.gameObject.activeSelf);
            }
        }
        
    }
}
