using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public static Transform Player_Transform;
    public static bool PlayerCanMove = true;
    public int MOVE_SPEED = 5;
    public Transform AchievementsPopupContainer;
    public GameObject AchievementBase; // should be organized using ui list layout
    private CanvasGroup AchievementCG;
    public AchievementTree Achievements; // cannot be written to after build, need to build a list/dict<int,bool[2]> of bools for isunlocked and isobtained
    public GameObject ItemObtainedBase;
    public Transform ItemObtainedContainer;
    private CanvasGroup ItemCG;
    public float AchievementFadeTime = 1;
    public float AchievementLifespan = 3;
    public string PlayerName;
    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 movement;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        Player_Transform = transform;
        AchievementsPopupContainer.TryGetComponent<CanvasGroup>(out AchievementCG);
        ItemObtainedContainer.TryGetComponent<CanvasGroup>(out ItemCG);
    }

    void Update()
    {
        if(PlayerCanMove) Move();
    }

    void Move()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        movement = new Vector2(moveX, moveY).normalized;
        rb.linearVelocity = movement * MOVE_SPEED;

        // todo: animate if moving
        if (movement.magnitude > 0.01f)
        {
            // Animate("Walk");
        }
        else
        {
            // Animate("Idle");
        }
    }

    void Animate(string animTrack)
    {
        if (animator != null)
        {
            animator.Play(animTrack);
        }
    }

    public void ObtainAchievement(string AchievementTitle)
    {
        AchievementNode achievementInfo = Achievements.GetAchievementNodeByTitle(AchievementTitle);
        if (!achievementInfo.isUnlocked) return;
        
        GameObject AchievementBaseClone = Instantiate(AchievementBase, AchievementsPopupContainer); // uilistlayout will animate.
        AchievementUIReference AUIRef = AchievementBaseClone.GetComponent<AchievementUIReference>();
        Image AchievementIconRenderer = AUIRef.AchievementIconRenderer;
        Image AchievementBorderRenderer = AUIRef.AchievementBorderRenderer;
        TextMeshProUGUI AchievementTitleText = AUIRef.AchievementTitleText;
        TextMeshProUGUI AchievementDescriptionText = AUIRef.AchievementDescriptionText;

        AchievementsPopupContainer.gameObject.SetActive(true);
        StopAllCoroutines();
        if(AchievementCG) {AchievementCG.alpha = 1; AchievementsPopupContainer.GetComponent<UIListLayout>().ApplyLayout(); StartCoroutine(FadeAchievements());}

        achievementInfo.IsObtained = true;
        foreach(string id in achievementInfo.NextAchievements)
        {
            AchievementNode a = (AchievementNode)Achievements.GetNode(id); a.isUnlocked = true;
        }

        if (achievementInfo.AchievementIcon != null) AchievementIconRenderer.sprite = achievementInfo.AchievementIcon;
        if (achievementInfo.IconBorder != null) AchievementBorderRenderer.sprite = achievementInfo.IconBorder;
        if (!string.IsNullOrEmpty(achievementInfo.TitleText)) AchievementTitleText.text = achievementInfo.TitleText;
        if (!string.IsNullOrEmpty(achievementInfo.DescriptionText)) AchievementDescriptionText.text = achievementInfo.DescriptionText;
    }
    IEnumerator FadeAchievements()
    {        
        yield return new WaitForSeconds(AchievementLifespan);
        float startAlpha = AchievementCG.alpha;
        float targetAlpha = 0f;
        float currentTime = 0f;

        while (currentTime < AchievementFadeTime)
        {
            currentTime += Time.deltaTime;
            AchievementCG.alpha = Mathf.Lerp(startAlpha, targetAlpha, currentTime / AchievementFadeTime);
            yield return null; 
        }
        foreach (Transform child in AchievementsPopupContainer)
        {
            Destroy(child.gameObject);
        }
        
        AchievementsPopupContainer.gameObject.SetActive(false);
    }
    
    Coroutine itemC;
    public void ObtainItem(string item, int count)
    {
        PlayerData.TryAddAmount(item, count);
        GameObject clone = Instantiate(ItemObtainedBase,ItemObtainedContainer);
        clone.GetComponent<Dialogue>().Play($"Obtained x{count} {item}");
        ItemObtainedContainer.gameObject.SetActive(true);
        if(itemC!=null)StopCoroutine(itemC);
        if(ItemCG) {ItemCG.alpha = 1; ItemObtainedContainer.GetComponent<UIListLayout>().ApplyLayout(); itemC=StartCoroutine(FadeItemUI());}
    }
    IEnumerator FadeItemUI()
    {
        yield return new WaitForSeconds(3);
        float startAlpha = 1;
        float targetAlpha = 0f;
        float currentTime = 0f;

        while (currentTime < 1)
        {
            currentTime += Time.deltaTime;
            ItemCG.alpha = Mathf.Lerp(startAlpha, targetAlpha, currentTime);
            yield return null; 
        }
        foreach (Transform child in AchievementsPopupContainer)
        {
            Destroy(child.gameObject);
        }
        
        AchievementsPopupContainer.gameObject.SetActive(false);
    }
    public void ObtainItems(Dictionary<string, int> items)
    {
        foreach(KeyValuePair<string,int> kv in items)
        {
            ObtainItem(kv.Key,kv.Value);
        }
    }
}
