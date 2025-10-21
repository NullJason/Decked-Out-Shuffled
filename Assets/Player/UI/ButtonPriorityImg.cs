using UnityEngine;
using UnityEngine.UI;

public class ButtonPriorityImg : MonoBehaviour
{
    Image img;
    void Start()
    {
        img = GetComponent<Image>();
    }
    public void SetButtonSideImage(Sprite sprite)
    {
        img.sprite = sprite;
    }
}
