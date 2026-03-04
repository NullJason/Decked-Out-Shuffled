using UnityEngine;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public SpriteRenderer cardSprite;
    public CardData cardData;
    
    public void SetCardData(CardData data)
    {
        cardData = data;
        
        if (nameText != null)
            nameText.text = data.cardName;
            
        if (descriptionText != null)
            descriptionText.text = data.description;
            
        if (cardSprite != null && data.cardSprite != null)
            cardSprite.sprite = data.cardSprite;
    }
    
    public void SetFaceUp(bool faceUp)
    {
        CardAnimate anim = GetComponent<CardAnimate>();
        if (anim != null && faceUp)
        {
            anim.Animate(1, 0.5f, true, false);
        }
    }
    
    public void SetColor(Color color)
    {
        if (cardSprite != null)
            cardSprite.color = color;
    }
}
