using UnityEngine;
using UnityEngine.UI;

public class ToggleEvent : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] GameObject toggable;
    void OnEnable()
    {
        if (button == null) TryGetComponent<Button>(out button);
        if (button != null && toggable != null) button.onClick.AddListener(DoEvent);
    }
    private void DoEvent()
    {
        toggable.SetActive(!toggable.activeSelf);
    }
}
