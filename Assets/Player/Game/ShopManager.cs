// ShopManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Header("Shop Stats")]
    public int funds = 100;
    public int rating = 50;
    public int stock = 50;
    public int day = 1;
    public int maxDays = 20;
    
    [Header("Thresholds")]
    public int minFunds = 20;
    public int minRating = 30;
    public int minStock = 20;
    
    [Header("Customers")]
    public int customersToday = 0;
    public int totalCustomersToday = 0;
    public List<Customer> waitingCustomers = new List<Customer>();
    public Transform customerSpawnPoint;
    public GameObject customerPrefab;
    
    [Header("Cards")]
    public GameObject decisionCardPrefab;
    public Transform cardSwipeArea;
    private GameObject currentCard;
    private Customer currentCustomer;
    
    [Header("References")]
    public ActionManager actionManager;
    public GameObject cardBattleOverlay;
    public BaseCardGameManager[] gameManagers;
    
    [Header("Persistent Data")]
    public static ShopSaveData savedData;
    
    void Start()
    {
        LoadSavedData();
        StartNewDay();
    }
    
    private void LoadSavedData()
    {
        if (savedData != null)
        {
            funds = savedData.funds;
            rating = savedData.rating;
            stock = savedData.stock;
            day = savedData.day;
        }
    }
    
    private void SaveData()
    {
        if (savedData == null)
            savedData = new ShopSaveData();
            
        savedData.funds = funds;
        savedData.rating = rating;
        savedData.stock = stock;
        savedData.day = day;
    }
    
    public void StartNewDay()
    {
        day++;
        
        if (day > maxDays)
        {
            EndGame();
            return;
        }
        
        // Generate customers for today
        totalCustomersToday = Random.Range(5, 9);
        customersToday = 0;
        
        // Spawn first customer
        SpawnNextCustomer();
    }
    
    private void SpawnNextCustomer()
    {
        if (customersToday >= totalCustomersToday)
        {
            EndDay();
            return;
        }
        
        customersToday++;
        
        // Create customer
        GameObject customerObj = Instantiate(customerPrefab, customerSpawnPoint.position, Quaternion.identity);
        currentCustomer = customerObj.GetComponent<Customer>();
        currentCustomer.Initialize(GenerateRandomRequest());
        
        // Create decision card
        CreateDecisionCard(currentCustomer.request);
    }
    
    private CustomerRequest GenerateRandomRequest()
    {
        CustomerRequest request = new CustomerRequest();
        
        // Random request type
        int requestType = Random.Range(0, 4);
        
        switch (requestType)
        {
            case 0: // Buy item
                request.question = "Can I buy this rare item?";
                request.acceptText = "Sell for $" + Random.Range(10, 50);
                request.declineText = "Sorry, not for sale";
                request.acceptEffects = new int[] { Random.Range(10, 50), 0, -5 }; // +funds, +rating, -stock
                request.declineEffects = new int[] { 0, -5, 0 }; // -rating
                request.canTriggerBattle = Random.value < 0.2f;
                break;
                
            case 1: // Complaint
                request.question = "This item I bought is broken!";
                request.acceptText = "Refund and replace";
                request.declineText = "No refunds";
                request.acceptEffects = new int[] { -20, 10, -1 }; // -funds, +rating, -stock
                request.declineEffects = new int[] { 0, -15, 0 }; // -rating
                request.canTriggerBattle = Random.value < 0.4f;
                break;
                
            case 2: // Special order
                request.question = "Can you order a special item for me?";
                request.acceptText = "Place order ($30)";
                request.declineText = "Can't do special orders";
                request.acceptEffects = new int[] { -30, 5, 10 }; // -funds, +rating, +stock (later)
                request.declineEffects = new int[] { 0, -10, 0 }; // -rating
                request.canTriggerBattle = false;
                break;
                
            case 3: // Trade
                request.question = "Want to trade for this card?";
                request.acceptText = "Trade stock for card";
                request.declineText = "No thanks";
                request.acceptEffects = new int[] { 0, 5, -15 }; // +rating, -stock
                request.declineEffects = new int[] { 0, -5, 0 }; // -rating
                request.canTriggerBattle = Random.value < 0.3f;
                break;
        }
        
        return request;
    }
    
    private void CreateDecisionCard(CustomerRequest request)
    {
        if (currentCard != null)
            Destroy(currentCard);
            
        currentCard = Instantiate(decisionCardPrefab, cardSwipeArea.position, Quaternion.identity, cardSwipeArea);
        
        DecisionCard card = currentCard.GetComponent<DecisionCard>();
        card.Initialize(request, this);
    }
    
    public void SwipeCard(bool accept)
    {
        if (currentCustomer == null) return;
        
        CustomerRequest request = currentCustomer.request;
        
        // Apply effects
        int[] effects = accept ? request.acceptEffects : request.declineEffects;
        funds += effects[0];
        rating += effects[1];
        stock += effects[2];
        
        // Clamp values
        funds = Mathf.Max(0, funds);
        rating = Mathf.Clamp(rating, 0, 100);
        stock = Mathf.Max(0, stock);
        
        // Check for card battle
        if (accept && request.canTriggerBattle)
        {
            StartCardBattle();
        }
        else
        {
            CompleteCustomer();
        }
    }
    
    private void StartCardBattle()
    {
        // Lock shop interaction
        LockShop(true);
        
        // Show card battle overlay
        cardBattleOverlay.SetActive(true);
        
        // Randomly select a game type
        int gameIndex = Random.Range(0, 3);
        BaseCardGameManager selectedGame = gameManagers[gameIndex];
        
        // Initialize mini-game
        selectedGame.currentEnvironment = (GameEnvironment)gameIndex;
        
        // Set up callback for when battle ends
        StartCoroutine(RunCardBattle(selectedGame));
    }
    
    private IEnumerator RunCardBattle(BaseCardGameManager game)
    {
        // Start the game
        game.StartGame();
        
        // Wait for game to finish
        while (game.isGameActive)
        {
            yield return null;
        }
        
        // Return to shop
        cardBattleOverlay.SetActive(false);
        LockShop(false);
        
        // Apply battle consequences
        // (win/loss effects could be applied here)
        
        CompleteCustomer();
    }
    
    private void CompleteCustomer()
    {
        // Animate customer leaving
        if (currentCustomer != null)
        {
            currentCustomer.Leave();
            Destroy(currentCustomer.gameObject, 1f);
        }
        
        // Destroy current card
        if (currentCard != null)
        {
            Destroy(currentCard);
        }
        
        // Next customer
        StartCoroutine(NextCustomerWithDelay());
    }
    
    private IEnumerator NextCustomerWithDelay()
    {
        yield return new WaitForSeconds(1f);
        SpawnNextCustomer();
    }
    
    private void EndDay()
    {
        // Check if stats are above thresholds
        if (funds < minFunds || rating < minRating || stock < minStock)
        {
            GameOver();
            return;
        }
        
        // Save progress
        SaveData();
        
        // Start next day
        StartCoroutine(StartNextDayWithDelay());
    }
    
    private IEnumerator StartNextDayWithDelay()
    {
        yield return new WaitForSeconds(2f);
        StartNewDay();
    }
    
    private void EndGame()
    {
        // Player survived all days
        if (actionManager != null)
        {
            actionManager.ExecuteAction("ShopWin");
        }
        
        // Award player
        var player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.ObtainItem("Card Embers", 25);
            player.ObtainItem("Soul Plasma", 25);
        }
    }
    
    private void GameOver()
    {
        // Player failed
        if (actionManager != null)
        {
            actionManager.ExecuteAction("ShopLose");
        }
    }
    
    private void LockShop(bool lockState)
    {
        // Disable/enable shop interactions
        var colliders = GetComponentsInChildren<Collider2D>();
        foreach (var collider in colliders)
        {
            collider.enabled = !lockState;
        }
    }
    
    void Update()
    {
        // Update UI displays
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        // Update UI elements with current stats
        // Implementation depends on your UI system
    }
}

// Customer.cs
public class Customer : MonoBehaviour
{
    public CustomerRequest request;
    private Animator animator;
    
    public void Initialize(CustomerRequest newRequest)
    {
        request = newRequest;
        animator = GetComponent<Animator>();
        
        if (animator != null)
        {
            animator.SetTrigger("Enter");
        }
    }
    
    public void Leave()
    {
        if (animator != null)
        {
            animator.SetTrigger("Leave");
        }
    }
}

// DecisionCard.cs
public class DecisionCard : MonoBehaviour
{
    private CustomerRequest request;
    private ShopManager shopManager;
    private bool isDragging = false;
    private Vector3 startPosition;
    private float swipeThreshold = 200f;
    
    public void Initialize(CustomerRequest newRequest, ShopManager manager)
    {
        request = newRequest;
        shopManager = manager;
        startPosition = transform.position;
        
        // Setup card text
        // Implementation depends on your UI
    }
    
    void Update()
    {
        if (isDragging)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            transform.position = mousePos;
        }
    }
    
    void OnMouseDown()
    {
        isDragging = true;
    }
    
    void OnMouseUp()
    {
        isDragging = false;
        
        float dragDistance = Vector3.Distance(transform.position, startPosition);
        
        if (dragDistance > swipeThreshold)
        {
            bool accept = transform.position.x > startPosition.x;
            shopManager.SwipeCard(accept);
        }
        else
        {
            // Return to start position
            StartCoroutine(ReturnToStart());
        }
    }
    
    private IEnumerator ReturnToStart()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 currentPos = transform.position;
        
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(currentPos, startPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = startPosition;
    }
}

// Data classes
[System.Serializable]
public class CustomerRequest
{
    public string question;
    public string acceptText;
    public string declineText;
    public int[] acceptEffects; // [funds, rating, stock]
    public int[] declineEffects;
    public bool canTriggerBattle;
}

[System.Serializable]
public class ShopSaveData
{
    public int funds;
    public int rating;
    public int stock;
    public int day;
}