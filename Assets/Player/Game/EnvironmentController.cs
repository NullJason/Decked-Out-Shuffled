
using UnityEngine;

public class EnvironmentController : MonoBehaviour
{
    public GameEnvironment currentEnvironment;
    
    public CardData ModifyCardForEnvironment(CardData originalCard)
    {
        CardData modifiedCard = Instantiate(originalCard);
        
        // Apply environment-specific modifications
        switch (currentEnvironment)
        {
            case GameEnvironment.Chess:
                modifiedCard = ApplyChessEnvironment(originalCard);
                break;
            case GameEnvironment.Uno:
                modifiedCard = ApplyUnoEnvironment(originalCard);
                break;
            case GameEnvironment.RPG:
                modifiedCard = ApplyRPGEnvironment(originalCard);
                break;
        }
        
        return modifiedCard;
    }
    
    private CardData ApplyChessEnvironment(CardData card)
    {
        if (card.cardType == "Uno")
        {
            // Uno cards in chess environment
            switch (card.cardValue)
            {
                case 14: // Joker
                    card.description = "Becomes a random chess piece card";
                    break;
                case 12: // Queen
                    card.description = "Move queen to any empty square";
                    break;
                case 13: // King
                    card.description = "Prevent check/checkmate next turn";
                    break;
                default:
                    card.description = "Invalid in chess - skip turn";
                    break;
            }
        }
        else if (card.cardType == "RPG")
        {
            // RPG cards in chess environment
            // Apply RPG->Chess conversions
        }
        
        return card;
    }
    
    private CardData ApplyUnoEnvironment(CardData card)
    {
        if (card.cardType == "Chess")
        {
            // Chess cards in Uno environment
            // Apply Chess->Uno conversions
        }
        else if (card.cardType == "RPG")
        {
            // RPG cards in Uno environment
            // Apply RPG->Uno conversions
        }
        
        return card;
    }
    
    private CardData ApplyRPGEnvironment(CardData card)
    {
        if (card.cardType == "Uno")
        {
            // Uno cards in RPG environment
            // Stats based on card value
            card.description = $"HP/ATK: {card.cardValue}" + 
                              (card.cardValue <= 5 ? " +1 Shield" : "");
        }
        else if (card.cardType == "Chess")
        {
            // Chess cards in RPG environment
            // Apply Chess->RPG stat conversions
        }
        
        return card;
    }
}