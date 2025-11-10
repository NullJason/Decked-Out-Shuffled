using System.Collections;
using UnityEngine;

public class CardAnimate : MonoBehaviour
{
    [SerializeField] Sprite CardSprite_Back;
    [SerializeField] Sprite CardSprite_Front;
    [SerializeField] float defaultAnimationDuration = 0.5f;
    
    bool isPlayingAnim = false;
    bool CanBeInterrupted;
    
    private SpriteRenderer spriteRenderer;
    private Coroutine currentAnimation;
    private Vector2 currentStartPos;
    private Quaternion originalRotation;
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        originalRotation = transform.rotation;
    }
    /// <summary>
    /// Animates a card, interpolating between start and end positions or rotation depending on method used, using cubic ease-in. 
    /// Behavior:
    /// if flip card is true, will slowly rotate the card 180 degrees on the y-axis. 
    /// For Y-rotation, When reaching 90 degrees, toggle the sprite from back to front or front to back, and set the rotation to -90 degrees so the other sprite isn't backwards.
    /// Rotation should always end at 0 after animation finishes.
    /// If a non-rotate animation interrupts a rotating animation it should continue to complete the current flip so Y-rot is 0
    /// </summary>
    /// <param name="StartPos">Uses this as the starting position if its not empty</param>
    /// <param name="EndPos">Where the card ends up at the end of the animation.</param>
    /// <param name="FlipCard"> if true, set the rotation of the card based on the current position of the animation.</param>
    /// <param name="Interrupt"> If true stops all other animations and plays this one instead, doesnt work if anim cannot be interrupted.
    /// <param name="LockAnim"> sets CanBeInterrupted to this, only works if its
    public void Animate(Vector2 StartPos, Vector2 EndPos, bool FlipCard = false, bool Interrupt = true, bool LockAnim = false)
    {
        if (!CanStartAnimation(Interrupt)) return;
        currentStartPos = StartPos;
        transform.position = StartPos;
        currentAnimation = StartCoroutine(AnimateCoroutine(StartPos, EndPos, FlipCard, LockAnim, defaultAnimationDuration));    }
    public void Animate(Vector2 EndPos, bool FlipCard = false, bool Interrupt = true, bool LockAnim = false)
    {
        if (!CanStartAnimation(Interrupt)) return;
        
        currentStartPos = (Vector2)transform.position;
        currentAnimation = StartCoroutine(AnimateCoroutine(currentStartPos, EndPos, FlipCard, LockAnim, defaultAnimationDuration));
    }
    // doesn't rotate the card, anims only position.
    public void Animate(Vector2 EndPos, bool Interrupt = true, bool LockAnim = false)
    {
        if (!CanStartAnimation(Interrupt)) return;
        
        currentStartPos = (Vector2)transform.position;
        currentAnimation = StartCoroutine(AnimateCoroutine(currentStartPos, EndPos, false, LockAnim, defaultAnimationDuration));
    }
    // anims position and rotation, will stop the rotation at StopRotation deg whenever it is hit.
    public void Animate(Vector2 EndPos, float StopRotation = 70, bool Interrupt = true, bool LockAnim = false)
    {
        if (!CanStartAnimation(Interrupt)) return;
        
        currentStartPos = (Vector2)transform.position;
        currentAnimation = StartCoroutine(AnimateWithStopRotation(currentStartPos, EndPos, StopRotation, LockAnim, defaultAnimationDuration));
    }
    // just flips the card a number of times, cubic ease-in as well.
    public void Animate(int NumFlips = 1, float TotalTime = 1, bool Interrupt = true, bool LockAnim = false)
    {
        if (!CanStartAnimation(Interrupt)) return;

        currentAnimation = StartCoroutine(AnimateFlipOnly(NumFlips, TotalTime, LockAnim));
    }
    private bool CanStartAnimation(bool Interrupt)
    {
        if (isPlayingAnim)
        {
            if (!CanBeInterrupted || !Interrupt)
                return false;
                
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
        }
        return true;
    }
    
    private IEnumerator AnimateCoroutine(Vector2 startPos, Vector2 endPos, bool flipCard, bool lockAnim, float duration)
    {
        isPlayingAnim = true;
        CanBeInterrupted = !lockAnim;
        
        float elapsed = 0f;
        bool hasFlipped = false;
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = originalRotation;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easedT = CubicEaseIn(t);
            
            transform.position = Vector2.Lerp(startPos, endPos, easedT);
            
            if (flipCard)
            {
                float rotation = Mathf.Lerp(0, 180, easedT);
                
                if (rotation >= 90f && !hasFlipped)
                {
                    spriteRenderer.sprite = (spriteRenderer.sprite == CardSprite_Back) ? CardSprite_Front : CardSprite_Back;
                    transform.rotation = Quaternion.Euler(0, -90, 0);
                    hasFlipped = true;
                }
                else if (rotation < 90f)
                {
                    transform.rotation = Quaternion.Euler(0, rotation, 0);
                }
                else if (hasFlipped)
                {
                    float remainingRotation = Mathf.Lerp(-90, 0, (t - 0.5f) * 2f);
                    transform.rotation = Quaternion.Euler(0, remainingRotation, 0);
                }
            }
            else
            {
                transform.rotation = Quaternion.Lerp(startRot, targetRot, easedT);
            }
            
            yield return null;
        }
        
        transform.position = endPos;
        transform.rotation = targetRot;
        
        isPlayingAnim = false;
        CanBeInterrupted = true;
    }
    
    private IEnumerator AnimateWithStopRotation(Vector2 startPos, Vector2 endPos, float stopRotation, bool lockAnim, float duration)
    {
        isPlayingAnim = true;
        CanBeInterrupted = !lockAnim;
        
        float elapsed = 0f;
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = Quaternion.Euler(0, stopRotation, 0);
        bool rotationReached = false;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easedT = CubicEaseIn(t);
            
            transform.position = Vector2.Lerp(startPos, endPos, easedT);
            
            if (!rotationReached)
            {
                transform.rotation = Quaternion.Lerp(startRot, targetRot, easedT);
                
                if (Mathf.Abs(transform.rotation.eulerAngles.y - stopRotation) < 1f)
                {
                    rotationReached = true;
                }
            }
            
            yield return null;
        }
        
        transform.position = endPos;
        isPlayingAnim = false;
        CanBeInterrupted = true;
    }
    
    private IEnumerator AnimateFlipOnly(int numFlips, float totalTime, bool lockAnim)
    {
        isPlayingAnim = true;
        CanBeInterrupted = !lockAnim;
        
        float elapsed = 0f;
        int flipsCompleted = 0;
        bool hasFlippedSprite = false;
        
        while (elapsed < totalTime && flipsCompleted < numFlips)
        {
            elapsed += Time.deltaTime;
            float flipProgress = (elapsed / totalTime) * numFlips;
            float currentFlipProgress = flipProgress - flipsCompleted;
            
            float easedT = CubicEaseIn(currentFlipProgress);
            float rotation = Mathf.Lerp(0, 180, easedT);
            
            if (rotation >= 90f && !hasFlippedSprite)
            {
                spriteRenderer.sprite = (spriteRenderer.sprite == CardSprite_Back) ? CardSprite_Front : CardSprite_Back;
                transform.rotation = Quaternion.Euler(0, -90, 0);
                hasFlippedSprite = true;
            }
            else if (rotation < 90f)
            {
                transform.rotation = Quaternion.Euler(0, rotation, 0);
            }
            else if (hasFlippedSprite)
            {
                float remainingRotation = Mathf.Lerp(-90, 0, (currentFlipProgress - 0.5f) * 2f);
                transform.rotation = Quaternion.Euler(0, remainingRotation, 0);
                
                if (remainingRotation >= 0f && Mathf.Abs(remainingRotation) < 1f)
                {
                    flipsCompleted++;
                    hasFlippedSprite = false;
                    transform.rotation = originalRotation;
                }
            }
            
            yield return null;
        }
        
        transform.rotation = originalRotation;
        
        isPlayingAnim = false;
        CanBeInterrupted = true;
    }
    
    
    
    private float CubicEaseIn(float t)
    {
        return t * t * t;
    }
}
