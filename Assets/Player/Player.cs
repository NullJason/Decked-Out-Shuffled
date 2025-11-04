using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class Player : MonoBehaviour
{
    public int MOVE_SPEED = 5;
    public Transform AchievementsPopupContainer;
    public GameObject AchievementBase; // should be organized using ui list layout
    public AchievementTree Achievements;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 movement;
    private Dictionary<string, int> Items; // not 4 cards

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        Move();
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

    public void ObtainAchievement(string AchievementID)
    {
        GameObject AchievementBaseClone = Instantiate(AchievementBase, AchievementsPopupContainer); // uilistlayout will animate.
        AchievementUIReference AUIRef = AchievementBaseClone.GetComponent<AchievementUIReference>();
        SpriteRenderer AchievementIconRenderer = AUIRef.AchievementIconRenderer;
        SpriteRenderer AchievementBorderRenderer = AUIRef.AchievementBorderRenderer;
        TextMeshProUGUI AchievementTitleText = AUIRef.AchievementTitleText;
        TextMeshProUGUI AchievementDescriptionText = AUIRef.AchievementDescriptionText;

        AchievementNode achievementInfo = Achievements.GetTypedNode(AchievementID);

        if (achievementInfo.AchievementIcon != null) AchievementIconRenderer.sprite = achievementInfo.AchievementIcon;
        if (achievementInfo.IconBorder != null) AchievementBorderRenderer.sprite = achievementInfo.IconBorder;
        if (!string.IsNullOrEmpty(achievementInfo.TitleText)) AchievementTitleText.text = achievementInfo.TitleText;
        if (!string.IsNullOrEmpty(achievementInfo.DescriptionText)) AchievementDescriptionText.text = achievementInfo.DescriptionText;
    }
    public void ObtainItem(string item, int count)
    {
        if(!Items.TryAdd(item, count)) Items[item]+=count;
    }
}
