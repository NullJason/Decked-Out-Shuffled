using System.Collections;
using UnityEngine;

public class JumpableObject : MonoBehaviour
{
    [SerializeField] private int ObjectHeight = 0;
    [SerializeField] private Collider2D PlatformArea;
    [SerializeField] private Transform PlatformStartPos; // Where player lands on the platform
    [SerializeField] private bool CanAccidentalFall = true;
    [SerializeField] private bool CanManualFall = true;
    [SerializeField] private Transform GroundPos; // land areaa
    [SerializeField] private bool UseXYAxis = false; // Use both X and Y for falling, or only Y
    [SerializeField] private float InteractionDistance = 3;
    [SerializeField] private Collider2D JOCollider;
    
    [Header("Animation")]
    [SerializeField] private float jumpHeight = 1f;
    [SerializeField] private float jumpDuration = 0.5f;

    public static KeyCode JOInteractKey = KeyCode.Space;

    private GameObject player;
    private Collider2D playerCollider;
    private SpriteRenderer playerSpriteRenderer;
    private bool isPlayerOnObject = false;
    private bool isJumping = false;
    private int playerOriginalSortOrder = 0;
    private Coroutine jumpCoroutine;
    private Coroutine fallCoroutine;
    private float lastInteractionTime = 0f;
    private float inputCooldown = 0.3f;

    void Start()
    {
        player = GameObject.FindFirstObjectByType<Player>().gameObject;
        
        if (player != null)
        {
            playerCollider = player.GetComponent<Collider2D>();
            playerSpriteRenderer = player.GetComponent<SpriteRenderer>();
        }

        if (PlatformArea == null)
            Debug.LogWarning("PlatformArea not assigned in JumpableObject: " + gameObject.name);

        if (PlatformStartPos == null)
            Debug.LogWarning("PlatformStartPos not assigned in JumpableObject: " + gameObject.name);

        if (GroundPos == null)
            Debug.LogWarning("GroundPos not assigned in JumpableObject: " + gameObject.name);
    }

    void Update()
    {
        if (player == null) return;

        float currentTime = Time.time;

        if (!isPlayerOnObject && !isJumping && CanPlayerJumpOn())
        {
            if (Input.GetKeyDown(JOInteractKey) && currentTime > lastInteractionTime + inputCooldown)
            {
                JumpOnObject();
                lastInteractionTime = currentTime;
            }
        }

        if (isPlayerOnObject && !isJumping)
        {
            HandleFalling();
        }
    }

    private bool CanPlayerJumpOn()
    {
        if (player == null) return false;

        float distance = Vector2.Distance(player.transform.position, transform.position);
        if (distance > InteractionDistance) return false;

        if (playerSpriteRenderer != null && playerSpriteRenderer.sortingOrder >= ObjectHeight - 1)
        {
            return true;
        }

        return false;
    }

    private void JumpOnObject()
    {
        if (player == null || PlatformStartPos == null) return;
        
        if (jumpCoroutine != null)
            StopCoroutine(jumpCoroutine);
        
        jumpCoroutine = StartCoroutine(JumpAnimation());
    }

    private IEnumerator JumpAnimation()
    {
        isJumping = true;
        if (JOCollider != null)
            JOCollider.isTrigger = true;
        
        if (playerSpriteRenderer != null)
        {
            playerOriginalSortOrder = playerSpriteRenderer.sortingOrder;
            playerSpriteRenderer.sortingOrder = ObjectHeight;
        }

        Player.PlayerCanMove = false;

        Vector3 startPos = player.transform.position;
        Vector3 platformLandingPos = PlatformStartPos.position;
        
        if (playerCollider != null)
        {
            startPos = playerCollider.bounds.center;
        }
        
        Vector3 jumpApex = Vector3.Lerp(startPos, platformLandingPos, 0.5f);
        jumpApex.y += jumpHeight;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < jumpDuration * 0.5f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (jumpDuration * 0.5f);
            t = t * t; 
            
            Vector3 position = Vector3.Lerp(
                Vector3.Lerp(startPos, jumpApex, t),
                Vector3.Lerp(jumpApex, platformLandingPos, t),
                t
            );
            
            player.transform.position = position;
            yield return null;
        }
        
        float remainingTime = jumpDuration * 0.5f;
        elapsedTime = 0f;
        
        while (elapsedTime < remainingTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / remainingTime;
            t = 1f - (1f - t) * (1f - t); 
            
            Vector3 position = Vector3.Lerp(
                Vector3.Lerp(jumpApex, platformLandingPos, t),
                Vector3.Lerp(platformLandingPos, platformLandingPos, t),
                t
            );
            
            player.transform.position = position;
            yield return null;
        }

        player.transform.position = platformLandingPos;
        
        Player.PlayerCanMove = true;
        isPlayerOnObject = true;
        isJumping = false;
        jumpCoroutine = null;
    }

    private void HandleFalling()
    {
        if (player == null || PlatformArea == null || fallCoroutine != null || isJumping) return;

        bool shouldFall = false;

        if (CanAccidentalFall)
        {
            Vector2 checkPoint;
            
            if (playerCollider != null)
            {
                checkPoint = playerCollider.bounds.center;
            }
            else
            {
                checkPoint = player.transform.position;
            }
            
            if (!PlatformArea.OverlapPoint(checkPoint))
            {
                shouldFall = true;
            }
        }

        if (CanManualFall && Input.GetKeyDown(JOInteractKey) && Time.time > lastInteractionTime + inputCooldown)
        {
            shouldFall = true;
            lastInteractionTime = Time.time;
        }

        if (shouldFall)
        {
            fallCoroutine = StartCoroutine(FallOffObject());
        }
    }

    private IEnumerator FallOffObject()
    {
        if (player == null || GroundPos == null) yield break;
        
        isPlayerOnObject = false;
        Player.PlayerCanMove = false;

        Vector3 startPos = player.transform.position;
        
        Vector3 endPos;
        if (UseXYAxis)
        {
            endPos = new Vector3(GroundPos.position.x, GroundPos.position.y, startPos.z);
        }
        else
        {
            endPos = new Vector3(startPos.x, GroundPos.position.y, startPos.z);
        }
        
        Vector3 fallApex = Vector3.Lerp(startPos, endPos, 0.3f);
        fallApex.y += 0.5f; 
        
        float fallDuration = 0.6f;
        float elapsedTime = 0f;
        
        while (elapsedTime < fallDuration * 0.4f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (fallDuration * 0.4f);
            t = t * t; 
            
            Vector3 position = Vector3.Lerp(
                Vector3.Lerp(startPos, fallApex, t),
                Vector3.Lerp(fallApex, endPos, t),
                t
            );
            
            player.transform.position = position;
            yield return null;
        }
        
        float remainingTime = fallDuration * 0.6f;
        elapsedTime = 0f;
        
        while (elapsedTime < remainingTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / remainingTime;
            t = 1f - Mathf.Pow(1f - t, 3);
            
            player.transform.position = Vector3.Lerp(
                player.transform.position, 
                endPos, 
                t * 0.5f 
            );
            yield return null;
        }

        player.transform.position = endPos;

        if (playerSpriteRenderer != null)
        {
            playerSpriteRenderer.sortingOrder = playerOriginalSortOrder;
        }

        Player.PlayerCanMove = true;
        
        if (JOCollider != null)
            JOCollider.isTrigger = false;

        fallCoroutine = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, InteractionDistance);

        if (PlatformArea != null)
        {
            Gizmos.color = Color.green;
            Bounds bounds = PlatformArea.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        if (PlatformStartPos != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(PlatformStartPos.position, 0.2f);
            Gizmos.DrawWireCube(PlatformStartPos.position, new Vector3(0.5f, 0.1f, 0.1f));
        }

        if (GroundPos != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(GroundPos.position, new Vector3(2f, 0.1f, 1f));
            
            if (UseXYAxis)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, GroundPos.position);
            }
        }
    }

    public void ForcePlayerFall()
    {
        if (isPlayerOnObject && fallCoroutine == null && !isJumping)
        {
            fallCoroutine = StartCoroutine(FallOffObject());
        }
    }

    public bool IsPlayerOnObject()
    {
        return isPlayerOnObject;
    }

    public int GetObjectHeight()
    {
        return ObjectHeight;
    }
}