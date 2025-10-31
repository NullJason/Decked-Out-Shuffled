using UnityEngine;
using UnityEngine.UI;

public class ButtonPriorityImg : MonoBehaviour
{
    public RectTransform ImgRect;
    public Image img;
    public Sprite MainImg;
    public Sprite SubImage;
    public Vector2 Main_Size;
    public Vector2 SubSize;
    public void SetButtonSideImage(bool main = false)
    {
        if (main) { img.sprite = MainImg; ImgRect.sizeDelta = Main_Size; }
        else { img.sprite = SubImage; ImgRect.sizeDelta = SubSize; }
    }
}
