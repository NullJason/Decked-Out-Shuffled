using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardUpgradeHandler : MonoBehaviour
{
    public static CardUpgradeHandler current;
    public int UpgradeCost = 4;
    public int UpgradeIncrement = 2;
    public TextMeshProUGUI costtext;
    public TextMeshProUGUI inventoryAmountText;
    public string template = "You have [{amount}] Card Embers";
    DataReflectorText itemData;
    const string currency = "Card Embers";
    void Start()
    {
        itemData = new DataReflectorText(currency, inventoryAmountText, template);
        PlayerData.AddDataReflector(itemData);

        var btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(OnClicked);
    }
    void OnClicked()
    {
        current = this;
        if (costtext != null) costtext.text = $"Cost:\n{UpgradeCost} Card Embers";
        itemData.UpdateText(PlayerData.GetAmount(currency));
    }
    public void DoUpgrade()
    {
        if(PlayerData.GetAmount(currency)<UpgradeCost) return;
        PlayerData.TryAddAmount(currency, -UpgradeCost);
        UpgradeCost*=UpgradeIncrement;
    }
}
