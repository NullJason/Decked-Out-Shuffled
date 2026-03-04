
using UnityEngine;

[CreateAssetMenu(fileName = "CardData", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    public int cardValue;
    public string cardType; // "Uno", "Chess", "RPG"
    public Color color = Color.white;
    public string specialType;
    public string description;
    public Sprite cardSprite;
    
    public string GetDescription()
    {
        if (!string.IsNullOrEmpty(description))
            return description;
            
        return $"{cardType} Card - Value: {cardValue}";
    }
}
