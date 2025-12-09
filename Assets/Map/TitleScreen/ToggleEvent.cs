using UnityEngine;
using UnityEngine.UI;

public class ToggleEvent : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] GameObject toggable;
    [SerializeField] bool DisableOtherChildren = false;
    [SerializeField] bool CanDisable = true;
    void OnEnable()
    {
        if (button == null) TryGetComponent<Button>(out button);
        if (button != null && toggable != null) button.onClick.AddListener(DoEvent);
    }
    private void DoEvent()
    {
        bool state = toggable.activeSelf;
        if(state && CanDisable) toggable.SetActive(false); else if(!state) toggable.SetActive(true);
        if(!DisableOtherChildren) return;
        foreach(Transform t in toggable.transform.parent)
        {
            if(t!=toggable.transform) t.gameObject.SetActive(false);
        }
    }
}
