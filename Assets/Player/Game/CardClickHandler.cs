
// CardClickHandler.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class CardClickHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public bool isLocked = false;
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isLocked) return;
        
        // Get current game manager
        BaseCardGameManager gameManager = FindFirstObjectByType<BaseCardGameManager>();
        if (gameManager != null)
        {
            // Open action menu based on game type
            ShowActionMenu(gameManager);
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        CardInfo info = GetComponent<CardInfo>();
        if (info != null)
        {
            info.OnPointerEnter();
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        CardInfo info = GetComponent<CardInfo>();
        if (info != null)
        {
            info.OnPointerExit();
        }
    }
    
    private void ShowActionMenu(BaseCardGameManager manager)
    {
        // Create action menu based on game type and card type
        // This would instantiate a UI panel with available actions
        
        CardDisplay display = GetComponent<CardDisplay>();
        if (display == null || display.cardData == null) return;
        
        // Example: Create action menu with relevant buttons
        // Implementation depends on your UI system
    }
}
