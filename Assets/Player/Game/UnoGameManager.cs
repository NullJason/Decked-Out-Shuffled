// UnoGameManager.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum UnoColor { Red, Blue, Green, Yellow, Wild }
public enum UnoCardType { Number, Skip, Reverse, DrawTwo, Wild, WildDrawFour, Custom }

public class UnoGameManager : BaseCardGameManager
{
    [Header("Uno Settings")]
    public UnoColor currentColor;
    public int currentNumber = -1;
    public bool directionClockwise = true;
    public int drawCount = 0;
    public bool saidUno = false;
    
    [Header("Card Values")]
    public Dictionary<int, UnoCardType> specialCards = new Dictionary<int, UnoCardType>()
    {
        {1, UnoCardType.Custom}, // Ace - Swap hands
        {8, UnoCardType.Wild}, // 8 - Wild card
        {10, UnoCardType.Skip}, // 10 - Skip
        {11, UnoCardType.Custom}, // Jack - See opponent cards
        {13, UnoCardType.DrawTwo}, // King - Draw Two
        {14, UnoCardType.Custom} // Joker - +5
    };
    
    [Header("Visuals")]
    public Color[] unoColors = new Color[4];
    
    private List<CardData> generatedDeck = new List<CardData>();
    private GameObject currentTopCard;
    
    public override void StartGame()
    {
        GenerateDeck();
        ShuffleDeck();
        DealCards();
        StartFirstTurn();
    }
    private void StartFirstTurn()
    {
        isPlayerTurn = true;
        actionLocked = false;
        Debug.Log("Player's turn starts!");
    }
    private void GenerateDeck()
    {
        // Generate standard poker cards 1-14
        // Make special cards 50% less common
        int[] specialValues = {1, 11, 12, 13, 14}; // Ace, Jack, Queen, King, Joker
        
        for (int color = 0; color < 4; color++) // 4 Uno colors
        {
            for (int value = 1; value <= 14; value++)
            {
                if (value <= 10 || value == 12) // Numbers 1-10 and Queen are more common
                {
                    // Add multiple copies of number cards
                    for (int i = 0; i < 2; i++) // Adjust for rarity
                    {
                        CardData card = CreateUnoCardData(value, (UnoColor)color);
                        generatedDeck.Add(card);
                    }
                }
                else if (specialValues.Contains(value))
                {
                    // Add fewer copies of special cards
                    CardData card = CreateUnoCardData(value, (UnoColor)color);
                    generatedDeck.Add(card);
                }
            }
        }
        
        // Add some wild cards (value 8)
        for (int i = 0; i < 4; i++)
        {
            CardData wildCard = CreateUnoCardData(8, UnoColor.Wild);
            generatedDeck.Add(wildCard);
        }
    }
    
    private CardData CreateUnoCardData(int value, UnoColor color)
    {
        CardData data = ScriptableObject.CreateInstance<CardData>();
        data.cardName = GetCardName(value, color);
        data.cardValue = value;
        data.cardType = "Uno";
        data.color = GetColorCode(color);
        data.specialType = GetSpecialType(value);
        
        // Set description based on value
        if (specialCards.ContainsKey(value))
        {
            switch (value)
            {
                case 1: data.description = "Swap hands with opponent"; break;
                case 8: data.description = "Wild card - choose color"; break;
                case 10: data.description = "Skip opponent's turn"; break;
                case 11: data.description = "See opponent's cards"; break;
                case 13: data.description = "+2 cards to opponent"; break;
                case 14: data.description = "+5 cards to opponent"; break;
                default: data.description = "Uno card"; break;
            }
        }
        else
        {
            data.description = $"Uno card - {value}";
        }
        
        return data;
    }
    
    private void ShuffleDeck()
    {
        for (int i = 0; i < generatedDeck.Count; i++)
        {
            CardData temp = generatedDeck[i];
            int randomIndex = Random.Range(i, generatedDeck.Count);
            generatedDeck[i] = generatedDeck[randomIndex];
            generatedDeck[randomIndex] = temp;
        }
    }
    
    private void DealCards()
    {
        // Deal 7 cards to each player
        for (int i = 0; i < 7; i++)
        {
            DrawCardForPlayer(true);
            DrawCardForPlayer(false);
        }
        
        // Setup first card
        DrawFirstCard();
    }
    
    private void DrawFirstCard()
    {
        if (generatedDeck.Count > 0)
        {
            CardData firstCard = generatedDeck[0];
            generatedDeck.RemoveAt(0);
            
            currentTopCard = CreateCard(firstCard, discardPilePosition, true);
            
            // Set current game state
            if (firstCard.color != Color.gray) // Not wild
            {
                currentColor = GetUnoColorFromColor(firstCard.color);
            }
            currentNumber = firstCard.cardValue;
            
            // Check if first card is special
            if (specialCards.ContainsKey(firstCard.cardValue))
            {
                ExecuteCardEffect(firstCard.cardValue, true);
            }
        }
    }
    
    public void PlayCard(GameObject cardObj, UnoColor chosenColor = UnoColor.Wild)
    {
        if (actionLocked || !isPlayerTurn || !isGameActive) return;
        
        CardDisplay display = cardObj.GetComponent<CardDisplay>();
        if (display == null) return;
        
        CardData cardData = display.cardData;
        
        // Check if card can be played
        if (CanPlayCard(cardData))
        {
            // Remove from hand
            playerHand.Remove(cardObj);
            
            // Move to discard
            MoveCardToDiscard(cardObj);
            currentTopCard = cardObj;
            
            // Update game state
            if (cardData.color != Color.gray) // Not wild
            {
                currentColor = GetUnoColorFromColor(cardData.color);
            }
            else if (chosenColor != UnoColor.Wild)
            {
                currentColor = chosenColor;
                // Change card color visually
                display.SetColor(GetColorCode(chosenColor));
            }
            
            currentNumber = cardData.cardValue;
            
            // Execute card effect
            if (specialCards.ContainsKey(cardData.cardValue))
            {
                ExecuteCardEffect(cardData.cardValue, false);
            }
            else
            {
                EndTurn();
            }
            
            // Check for UNO
            if (playerHand.Count == 1 && !saidUno)
            {
                // Player must say UNO or draw 2 cards
                DrawCardsForPlayer(true, 2);
            }
            else if (playerHand.Count == 0)
            {
                PlayerWins();
            }
        }
    }
    
    private bool CanPlayCard(CardData card)
    {
        // Wild cards can always be played
        if (card.cardValue == 8 || card.cardValue == 14) // Wild or Joker
            return true;
            
        // Color match
        if (card.color == GetColorCode(currentColor))
            return true;
            
        // Number match
        if (card.cardValue == currentNumber)
            return true;
            
        return false;
    }
    
    private void ExecuteCardEffect(int value, bool isFirstCard)
    {
        switch (value)
        {
            case 1: // Ace - Swap hands
                if (!isFirstCard)
                {
                    StartCoroutine(SwapHands());
                }
                break;
                
            case 8: // Wild
                if (!isFirstCard)
                {
                    // Show color selection UI
                    ShowColorSelection();
                    LockTurn(true);
                }
                break;
                
            case 10: // Skip
                if (!isFirstCard)
                {
                    SkipNextTurn();
                }
                break;
                
            case 11: // Jack - See opponent cards
                if (!isFirstCard)
                {
                    StartCoroutine(RevealOpponentCards());
                }
                break;
                
            case 13: // King - Draw Two
                if (!isFirstCard)
                {
                    int target = directionClockwise ? 0 : 1; // 0 = opponent, 1 = player
                    DrawCardsForPlayer(target == 0, 2);
                }
                break;
                
            case 14: // Joker - +5
                if (!isFirstCard)
                {
                    int target = directionClockwise ? 0 : 1;
                    DrawCardsForPlayer(target == 0, 5);
                }
                break;
        }
    }
    
    private IEnumerator SwapHands()
    {
        LockTurn(true);
        
        // Animate swapping
        List<GameObject> temp = new List<GameObject>(playerHand);
        playerHand = new List<GameObject>(opponentHand);
        opponentHand = temp;
        
        // Update card positions
        yield return StartCoroutine(RepositionHands());
        
        LockTurn(false);
        EndTurn();
    }
    
    private IEnumerator RevealOpponentCards()
    {
        LockTurn(true);
        
        foreach (GameObject card in opponentHand)
        {
            CardAnimate anim = card.GetComponent<CardAnimate>();
            if (anim != null)
            {
                anim.Animate(1, 0.5f, true, false);
            }
        }
        
        yield return new WaitForSeconds(3f);
        
        // Hide opponent cards again
        foreach (GameObject card in opponentHand)
        {
            CardAnimate anim = card.GetComponent<CardAnimate>();
            if (anim != null)
            {
                anim.Animate(1, 0.5f, true, false);
            }
        }
        
        LockTurn(false);
        EndTurn();
    }
    
    public void DrawCardsForPlayer(bool isPlayer, int count)
    {
        for (int i = 0; i < count; i++)
        {
            DrawCardForPlayer(isPlayer);
        }
    }
    
    private void DrawCardForPlayer(bool isPlayer)
    {
        if (generatedDeck.Count == 0) return;
        
        CardData card = generatedDeck[0];
        generatedDeck.RemoveAt(0);
        
        GameObject cardObj = CreateCard(card, 
            isPlayer ? playerHandArea : opponentHandArea,
            isPlayer);
            
        if (isPlayer)
        {
            playerHand.Add(cardObj);
        }
        else
        {
            opponentHand.Add(cardObj);
        }
    }
    
    public override void EndTurn()
    {
        isPlayerTurn = !isPlayerTurn;
        saidUno = false;
        
        if (!isPlayerTurn)
        {
            StartCoroutine(NPCTurn());
        }
    }
    
    private IEnumerator NPCTurn()
    {
        yield return new WaitForSeconds(1f);
        
        // NPC decision making
        List<GameObject> playableCards = new List<GameObject>();
        
        foreach (GameObject card in opponentHand)
        {
            CardDisplay display = card.GetComponent<CardDisplay>();
            if (display != null && CanPlayCard(display.cardData))
            {
                playableCards.Add(card);
            }
        }
        
        if (playableCards.Count > 0)
        {
            // Weighted random selection
            GameObject chosenCard = WeightedNPCDecision(playableCards);
            
            // Play the card
            CardDisplay display = chosenCard.GetComponent<CardDisplay>();
            PlayNPCCard(chosenCard, display.cardData);
        }
        else
        {
            // Draw card
            DrawCardForPlayer(false);
            EndTurn();
        }
    }
    
    private GameObject WeightedNPCDecision(List<GameObject> cards)
    {
        // Calculate move values
        Dictionary<GameObject, float> moveValues = new Dictionary<GameObject, float>();
        
        foreach (GameObject card in cards)
        {
            CardDisplay display = card.GetComponent<CardDisplay>();
            float value = CalculateCardValue(display.cardData);
            moveValues[card] = value;
        }
        
        // Sort by value
        var sorted = moveValues.OrderByDescending(x => x.Value).ToList();
        
        // Apply weights
        float[] weights = { 0.55f, 0.25f, 0.15f, 0.05f };
        
        float random = Random.value;
        float cumulative = 0f;
        
        for (int i = 0; i < Mathf.Min(sorted.Count, weights.Length); i++)
        {
            cumulative += weights[i];
            if (random <= cumulative)
            {
                return sorted[i].Key;
            }
        }
        
        return cards[0];
    }
    
    private float CalculateCardValue(CardData card)
    {
        float value = 0f;
        
        // Prefer cards that reduce opponent's hand
        if (card.cardValue == 13 || card.cardValue == 14) // Draw cards
            value += 3f;
            
        // Prefer wild cards
        if (card.cardValue == 8)
            value += 2f;
            
        // Prefer skip
        if (card.cardValue == 10)
            value += 1.5f;
            
        return value;
    }
    
    private void PlayNPCCard(GameObject card, CardData data)
    {
        opponentHand.Remove(card);
        MoveCardToDiscard(card);
        currentTopCard = card;
        
        // Update game state
        if (data.color != Color.gray)
        {
            currentColor = GetUnoColorFromColor(data.color);
        }
        else
        {
            // NPC chooses color
            currentColor = (UnoColor)Random.Range(0, 4);
        }
        
        currentNumber = data.cardValue;
        
        // Execute effect
        if (specialCards.ContainsKey(data.cardValue))
        {
            ExecuteCardEffect(data.cardValue, false);
        }
        else
        {
            EndTurn();
        }
    }
    
    private void SkipNextTurn()
    {
        isPlayerTurn = !isPlayerTurn; // Skip one turn
        EndTurn();
    }
    
    private void ShowColorSelection()
    {
        // Implementation for color selection UI
        // This would trigger a UI panel with 4 color buttons
    }
    
    public void SelectColor(UnoColor color)
    {
        currentColor = color;
        LockTurn(false);
        EndTurn();
    }
    
    private void PlayerWins()
    {
        isGameActive = false;
        AwardPlayer();
        if (actionManager != null)
        {
            actionManager.ExecuteAction("UnoWin");
        }
    }
    
    public override void CheckWinCondition()
    {
        if (playerHand.Count == 0)
        {
            PlayerWins();
        }
        else if (opponentHand.Count == 0)
        {
            // NPC wins
            isGameActive = false;
            if (actionManager != null)
            {
                actionManager.ExecuteAction("UnoLose");
            }
        }
    }
    
    // Helper methods
    private UnoColor GetUnoColorFromColor(Color color)
    {
        // Convert Unity Color to UnoColor
        if (color == Color.red) return UnoColor.Red;
        if (color == Color.blue) return UnoColor.Blue;
        if (color == Color.green) return UnoColor.Green;
        if (color == Color.yellow) return UnoColor.Yellow;
        return UnoColor.Wild;
    }
    
    private Color GetColorCode(UnoColor color)
    {
        switch (color)
        {
            case UnoColor.Red: return Color.red;
            case UnoColor.Blue: return Color.blue;
            case UnoColor.Green: return Color.green;
            case UnoColor.Yellow: return Color.yellow;
            default: return Color.gray;
        }
    }
    
    private string GetCardName(int value, UnoColor color)
    {
        string valueName = value switch
        {
            1 => "Ace",
            11 => "Jack",
            12 => "Queen",
            13 => "King",
            14 => "Joker",
            _ => value.ToString()
        };
        
        return $"{color} {valueName}";
    }
    
    private string GetSpecialType(int value)
    {
        if (specialCards.ContainsKey(value))
            return specialCards[value].ToString();
        return "Number";
    }
    
    private IEnumerator RepositionHands()
    {
        // Reposition player hand
        float spacing = 1.5f;
        for (int i = 0; i < playerHand.Count; i++)
        {
            Vector3 targetPos = new Vector3(i * spacing - (playerHand.Count * spacing / 2), 0, 0);
            CardAnimate anim = playerHand[i].GetComponent<CardAnimate>();
            if (anim != null)
            {
                anim.Animate(targetPos, true, false);
            }
            yield return new WaitForSeconds(0.1f);
        }
        
        // Reposition opponent hand
        for (int i = 0; i < opponentHand.Count; i++)
        {
            Vector3 targetPos = new Vector3(i * spacing - (opponentHand.Count * spacing / 2), 5, 0);
            CardAnimate anim = opponentHand[i].GetComponent<CardAnimate>();
            if (anim != null)
            {
                anim.Animate(targetPos, true, false);
            }
            yield return new WaitForSeconds(0.1f);
        }
    }
}