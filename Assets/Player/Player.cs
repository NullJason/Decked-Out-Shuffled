using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Transform Player_Transform;
    public static bool PlayerCanMove = true;
    public int MOVE_SPEED = 5;
    public Transform AchievementsPopupContainer;
    public GameObject AchievementBase; // should be organized using ui list layout
    public AchievementTree Achievements;
    public string PlayerName;
    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 movement;
    private Dictionary<string, int> Items; // not for cards. for other items

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        Player_Transform = transform;
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

    public void ObtainAchievement(string AchievementID)
    {
        AchievementNode achievementInfo = (AchievementNode)Achievements.GetNode(AchievementID);
        if (!achievementInfo.isUnlocked) return;
        
        GameObject AchievementBaseClone = Instantiate(AchievementBase, AchievementsPopupContainer); // uilistlayout will animate.
        AchievementUIReference AUIRef = AchievementBaseClone.GetComponent<AchievementUIReference>();
        SpriteRenderer AchievementIconRenderer = AUIRef.AchievementIconRenderer;
        SpriteRenderer AchievementBorderRenderer = AUIRef.AchievementBorderRenderer;
        TextMeshProUGUI AchievementTitleText = AUIRef.AchievementTitleText;
        TextMeshProUGUI AchievementDescriptionText = AUIRef.AchievementDescriptionText;

        achievementInfo.IsObtained = true;
        foreach(string id in achievementInfo.NextAchievements)
        {
            AchievementNode a=(AchievementNode)Achievements.GetNode(id); a.isUnlocked = true;
        }

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
