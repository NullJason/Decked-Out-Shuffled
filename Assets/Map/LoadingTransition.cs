using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Placed in TransitionCanvas
/// </summary>
public class LoadingTransition : MonoBehaviour
{
    [SerializeField] private int TransitionLifeSpan = 3;
    [SerializeField] private float FadeOutDuration = 1;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private List<GameObject> Card_Prefabs;
    [SerializeField] private float Max_FallSpeed = 1;
    [SerializeField] private float Min_FallSpeed = .1f;
    [SerializeField] private int CardsPerSecond = 1; // num cards that should spawn inside the canvas already every second. 3 cardspersecond = every 1/3 a second spawn a card.
    [SerializeField] private int Max_CardsAnimating = 10;
    [SerializeField] private Transform SpawnArea; // use position and size, cards will spawn randomly inside this area.
    [SerializeField] private Transform StartPos;
    [SerializeField] private Transform EndPos;
    [SerializeField] private float SpreadAngle = 100; // direction the cards go to are influenced by this.
    [SerializeField] private float CircularInfluence = 10; // from 0-100, causes cards to emulate gravity (gravitational influence) around the midpoint between start and end pos
    [SerializeField] private float LifeSpan = 3;
    [SerializeField] private List<CardAnimate> activeCards = new List<CardAnimate>();
    float start_t;
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        start_t = Time.time;
        group.alpha = 1;
        StartCoroutine(StartDroppingCards());
    }
    
    void OnDisable()
    {
        StopAllCoroutines();
        CleanupAllCards();
    }
    
    /// <summary>
    /// Uses CardAnimate.cs to animate the card.
    /// </summary>
    IEnumerator StartDroppingCards()
    {
        float spawnInterval = 1f / CardsPerSecond;
        
        while (true && Time.time - start_t < TransitionLifeSpan)
        {
            activeCards.RemoveAll(card => card == null);
            
            if (activeCards.Count < Max_CardsAnimating)
            {
                SpawnCard();
            }
            
            yield return new WaitForSeconds(spawnInterval);
        }
        float timer = 0f;
        float targetAlpha = 0;
        while (timer < FadeOutDuration)
        {
            timer += Time.deltaTime;
            float t = timer/FadeOutDuration;
            float easedT = t*t; 

            group.alpha = Mathf.Lerp(1, targetAlpha, easedT);
            yield return null;
        }
        gameObject.SetActive(false);
    }
    
    private void SpawnCard()
    {
        if (Card_Prefabs == null || Card_Prefabs.Count == 0)
        {
            Debug.LogWarning("No card prefabs assigned!");
            return;
        }
        
        GameObject cardPrefab = Card_Prefabs[Random.Range(0, Card_Prefabs.Count)];
        
        Vector2 spawnPosition = GetRandomSpawnPosition();
        
        GameObject cardObject = Instantiate(cardPrefab, spawnPosition, Quaternion.identity, transform);
        CardAnimate cardAnimate = cardObject.GetComponent<CardAnimate>();
        
        if (cardAnimate != null)
        {
            activeCards.Add(cardAnimate);
            
            Vector2 targetPosition = CalculateTargetPosition(spawnPosition);
            float fallSpeed = Random.Range(Min_FallSpeed, Max_FallSpeed);
            float animationDuration = CalculateAnimationDuration(spawnPosition, targetPosition, fallSpeed);
            
            bool shouldFlip = Random.Range(0, 2) == 1;
            
            cardAnimate.Animate(spawnPosition, targetPosition, shouldFlip, true, false);
            
            StartCoroutine(CleanupCardAfterAnimation(cardAnimate, animationDuration));
        }
        else
        {
            Debug.LogWarning("Spawned card doesn't have CardAnimate component!");
            Destroy(cardObject);
        }
    }
    
    private Vector2 GetRandomSpawnPosition()
    {
        if (SpawnArea != null)
        {
            RectTransform rectTransform = SpawnArea as RectTransform;
            if (rectTransform != null)
            {
                Vector2 center = rectTransform.position;
                Vector2 size = rectTransform.rect.size;
                
                float randomX = Random.Range(center.x - size.x / 2, center.x + size.x / 2);
                float randomY = Random.Range(center.y - size.y / 2, center.y + size.y / 2);
                
                return new Vector2(randomX, randomY);
            }
            else
            {
                Vector3 scale = SpawnArea.lossyScale;
                Vector3 position = SpawnArea.position;
                
                float randomX = Random.Range(position.x - scale.x / 2, position.x + scale.x / 2);
                float randomY = Random.Range(position.y - scale.y / 2, position.y + scale.y / 2);
                
                return new Vector2(randomX, randomY);
            }
        }
        else if (StartPos != null)
        {
            return StartPos.position;
        }
        else
        {
            return Vector2.zero;
        }
    }
    
    private Vector2 CalculateTargetPosition(Vector2 startPosition)
    {
        Vector2 baseTarget = EndPos != null ? (Vector2)EndPos.position : Vector2.zero;
        
        if (SpreadAngle > 0)
        {
            float spreadRad = SpreadAngle * Mathf.Deg2Rad;
            float randomAngle = Random.Range(-spreadRad / 2, spreadRad / 2);
            
            Vector2 direction = (baseTarget - startPosition).normalized;
            Vector2 spreadDirection = new Vector2(
                direction.x * Mathf.Cos(randomAngle) - direction.y * Mathf.Sin(randomAngle),
                direction.x * Mathf.Sin(randomAngle) + direction.y * Mathf.Cos(randomAngle)
            );
            
            float distance = Vector2.Distance(startPosition, baseTarget);
            baseTarget = startPosition + spreadDirection * distance;
        }
        
        if (CircularInfluence > 0 && EndPos != null && StartPos != null)
        {
            Vector2 midpoint = ((Vector2)StartPos.position + (Vector2)EndPos.position) / 2;
            float influenceStrength = CircularInfluence / 100f;
            
            Vector2 toCurrent = startPosition - midpoint;
            float distanceToMidpoint = toCurrent.magnitude;
            
            if (distanceToMidpoint > 0.1f)
            {
                Vector2 curvature = (midpoint - baseTarget) * influenceStrength * 0.1f;
                baseTarget += curvature;
            }
        }
        
        return baseTarget;
    }
    
    private float CalculateAnimationDuration(Vector2 startPosition, Vector2 endPosition, float speed)
    {
        float distance = Vector2.Distance(startPosition, endPosition);
        return distance / speed;
    }
    
    private IEnumerator CleanupCardAfterAnimation(CardAnimate card, float delay)
    {
        if (LifeSpan >= delay) yield return new WaitForSeconds(delay);
        else yield return new WaitForSeconds(LifeSpan);
        
        if (card != null)
        {
            activeCards.Remove(card);
            Destroy(card.gameObject);
        }
    }
    
    private void CleanupAllCards()
    {
        foreach (CardAnimate card in activeCards)
        {
            if (card != null)
            {
                Destroy(card.gameObject);
            }
        }
        activeCards.Clear();
    }
}
