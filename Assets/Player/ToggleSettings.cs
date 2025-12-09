using UnityEngine;

public class ToggleSettings : MonoBehaviour
{
    [SerializeField] GameObject toggable;
    [SerializeField] KeyCode keyCode = KeyCode.Escape;
    void Update()
    {
        if (Input.GetKeyDown(keyCode))
        {
            DoEvent();
        }
    }
    private void DoEvent()
    {
        if(toggable!=null) toggable.SetActive(!toggable.activeSelf);
    }

}
