
using UnityEngine;
using TMPro;

public class CardInfo : MonoBehaviour
{
    public GameObject CardInfoContainer;
    public TextMeshProUGUI CardTitleText;
    public TextMeshProUGUI CardDescText;
    public string item_title;
    public string item_desc;
    public float HoverTimeToDisplay = 2f;
    
    private float hoverTimer = 0f;
    private bool isHovering = false;
    
    void Start()
    {
        if (CardInfoContainer != null)
            CardInfoContainer.SetActive(false);
    }
    
    void Update()
    {
        if (isHovering)
        {
            hoverTimer += Time.deltaTime;
            if (hoverTimer >= HoverTimeToDisplay && CardInfoContainer != null)
            {
                CardInfoContainer.SetActive(true);
                if (CardTitleText != null) CardTitleText.text = item_title;
                if (CardDescText != null) CardDescText.text = item_desc;
            }
        }
    }
    
    public void OnPointerEnter()
    {
        isHovering = true;
    }
    
    public void OnPointerExit()
    {
        isHovering = false;
        hoverTimer = 0f;
        if (CardInfoContainer != null)
            CardInfoContainer.SetActive(false);
    }
    
    public void OnClick()
    {
        isHovering = false;
        hoverTimer = 0f;
        if (CardInfoContainer != null)
            CardInfoContainer.SetActive(false);
    }
}