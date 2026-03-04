// BaseCardGameManager.cs
using System.Collections.Generic;
using UnityEngine;

public enum GameEnvironment { Uno, Chess, RPG, Shop, None }

public abstract class BaseCardGameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public GameEnvironment currentEnvironment;
    public bool isPlayerTurn = true;
    public bool isGameActive = true;
    public bool actionLocked = false;
    
    [Header("Card Management")]
    public List<GameObject> playerHand = new List<GameObject>();
    public List<GameObject> opponentHand = new List<GameObject>();
    public List<GameObject> drawPile = new List<GameObject>();
    public List<GameObject> discardPile = new List<GameObject>();
    
    [Header("References")]
    public Transform playerHandArea;
    public Transform opponentHandArea;
    public Transform drawPilePosition;
    public Transform discardPilePosition;
    public GameObject cardPrefab;
    public ActionManager actionManager;
    
    [Header("Rewards")]
    public int cardEmbersReward = 10;
    public int soulPlasmaReward = 5;
    
    protected void Awake()
    {
        if (actionManager == null)
            actionManager = FindFirstObjectByType<ActionManager>();
    }
    
    public abstract void StartGame();
    public abstract void EndTurn();
    public abstract void CheckWinCondition();
    
    public void LockTurn(bool lockState)
    {
        actionLocked = lockState;
    }
    
    public void LockCardAction(GameObject card, bool lockState)
    {
        var cardClick = card.GetComponent<CardClickHandler>();
        if (cardClick != null)
        {
            cardClick.isLocked = lockState;
        }
    }
    
    public virtual void QuitGame()
    {
        if (actionManager != null)
        {
            actionManager.ExecuteAction("GameQuit");
        }
        else
        {
            // Fallback if no action manager
            Debug.Log("Game quit - no action manager found");
        }
    }
    
    protected void AwardPlayer()
    {
        var player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.ObtainItem("Card Embers", cardEmbersReward);
            player.ObtainItem("Soul Plasma", soulPlasmaReward);
        }
    }
    
    protected GameObject CreateCard(CardData data, Transform parent, bool isFaceUp)
    {
        GameObject cardObj = Instantiate(cardPrefab, parent);
        cardObj.transform.localPosition = Vector3.zero;
        
        // Set card data
        CardDisplay display = cardObj.GetComponent<CardDisplay>();
        if (display != null)
        {
            display.SetCardData(data);
            display.SetFaceUp(isFaceUp);
        }
        
        // Set card info
        CardInfo info = cardObj.GetComponent<CardInfo>();
        if (info != null && data != null)
        {
            info.item_title = data.cardName;
            info.item_desc = data.GetDescription();
        }
        
        return cardObj;
    }
    
    protected void MoveCardToDiscard(GameObject card)
    {
        CardAnimate anim = card.GetComponent<CardAnimate>();
        if (anim != null)
        {
            anim.Animate(discardPilePosition.position, true, false);
        }
        else
        {
            card.transform.position = discardPilePosition.position;
        }
        discardPile.Add(card);
    }
}