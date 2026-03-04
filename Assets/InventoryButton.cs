using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryButton : MonoBehaviour
{
    [Header("General")]
    public string item_id; 
    public string item_title;
    public string item_desc;
    public string template = "You have [{amount}]";
    public TextMeshProUGUI title;
    public TextMeshProUGUI desc;
    public TextMeshProUGUI amount;
    [Header("Visual Effect if count is 0")]
    public GameObject container;
    public Color32 Color_NoItem = new Color32(255,255,255,115);
    public Color32 Color_HasItem = new Color32(255,255,255,255);
    Image img;

    private DataReflectorText itemData;

    void Start()
    {
        // string template = amount != null ? amount.text : "{amount}";
        itemData = new DataReflectorText(item_id, amount, template);
        PlayerData.AddDataReflector(itemData);

        var btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(OnClicked);
    }

    void OnEnable()
    {
        if(container != null)
        {
            if(img == null) if(!container.TryGetComponent<Image>(out img)) return;
            int amt = PlayerData.GetAmount(item_id);
            if(amt != 0) img.color = Color_HasItem;
            else img.color = Color_NoItem;
        }

    }

    void OnClicked()
    {
        if (title != null) title.text = item_title;
        if (desc != null) desc.text = item_desc;
        itemData.UpdateText(PlayerData.GetAmount(item_id));
    }

    void OnDestroy()
    {
        if (itemData != null) PlayerData.RemoveDataReflector(itemData);
    }
}
