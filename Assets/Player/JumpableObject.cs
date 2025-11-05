using System.Collections;
using UnityEngine;
public class JumpableObject : MonoBehaviour
{
    // player needs to be at least this-1 sort order to jump onto this object. player sort order will be set to this if they can jump on.
    [SerializeField] private int ObjectHeight = 0;
    // player must stay within this area if they don't wish to fall. they can also press space bar to manually fall.
    [SerializeField] private Collider2D PlatformArea;
    [SerializeField] private bool CanAccidentalFall = true; // if going out of platform area causes them to fall
    [SerializeField] private bool CanManualFall = true; // 
    [SerializeField] private Transform GroundPos; // the y-value player will land on if they fall off platform. will use ease-in anim for pos.
    [SerializeField] private float InteractionDistance = 3;
    [SerializeField] private Collider2D JOCollider;
    public static KeyCode JOInteractKey = KeyCode.Space;

    private GameObject player;
    private bool isPlayerOnObject = false;
    private int playerOriginalSortOrder = 0;
    private Coroutine fallCoroutine;

    void Start()
    {
        player = GameObject.FindFirstObjectByType<Player>().gameObject;

        if (PlatformArea == null)
        {
            Debug.LogWarning("PlatformArea not assigned in JumpableObject: " + gameObject.name);
        }

        if (GroundPos == null)
        {
            Debug.LogWarning("GroundPos not assigned in JumpableObject: " + gameObject.name);
        }
    }

    void Update()
    {
        if (player == null) return;

        if (!isPlayerOnObject && CanPlayerJumpOn())
        {
            // prompt here. prob text that displays "[Space]"

            if (Input.GetKeyDown(JOInteractKey))
            {
                JumpOnObject();
            }
        }

        if (isPlayerOnObject)
        {
            HandleFalling();
        }
    }

    private bool CanPlayerJumpOn()
    {
        if (player == null) return false;

        float distance = Vector2.Distance(player.transform.position, transform.position);
        if (distance > InteractionDistance) return false;

        SpriteRenderer playerSprite = player.GetComponent<SpriteRenderer>();
        if (playerSprite != null && playerSprite.sortingOrder >= ObjectHeight - 1)
        {
            return true;
        }

        return false;
    }

    private void JumpOnObject()
    {
        if (player == null || PlatformArea == null) return;
        JOCollider.isTrigger = true;
        SpriteRenderer playerSprite = player.GetComponent<SpriteRenderer>();
        if (playerSprite != null)
        {
            playerSprite.sortingOrder = ObjectHeight;
        }

        Vector3 platformPosition = PlatformArea.bounds.center;
        player.transform.position = new Vector3(platformPosition.x, platformPosition.y, player.transform.position.z);

        isPlayerOnObject = true;
    }

    private void HandleFalling()
    {
        if (player == null || PlatformArea == null) return;

        bool shouldFall = false;

        if (CanAccidentalFall && !PlatformArea.bounds.Contains(player.transform.position))
        {
            shouldFall = true;
        }

        if (CanManualFall && Input.GetKeyDown(JOInteractKey))
        {
            shouldFall = true;
        }

        if (shouldFall && fallCoroutine == null)
        {
            fallCoroutine = StartCoroutine(FallOffObject());
        }
    }

    private IEnumerator FallOffObject()
    {
        if (player == null || GroundPos == null) yield break;
        JOCollider.isTrigger = false;

        Vector3 startPos = player.transform.position;
        Vector3 endPos = new Vector3(startPos.x, GroundPos.position.y, startPos.z);
        float fallDuration = 0.5f;
        float elapsedTime = 0f;

        while (elapsedTime < fallDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fallDuration;
            t = t * t;
            player.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        SpriteRenderer playerSprite = player.GetComponent<SpriteRenderer>();
        if (playerSprite != null)
        {
            playerSprite.sortingOrder = playerOriginalSortOrder;
        }

        isPlayerOnObject = false;
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

        if (GroundPos != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(GroundPos.position, new Vector3(2f, 0.1f, 1f));
        }
    }

    public void ForcePlayerFall()
    {
        if (isPlayerOnObject && fallCoroutine == null)
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

