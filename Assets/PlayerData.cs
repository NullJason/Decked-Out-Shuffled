using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;

public static class PlayerData
{
    private static readonly Dictionary<string, int> values = new Dictionary<string, int>(); // stores items and card quantities

    public static readonly Dictionary<string, List<DataReflectorText>> reflectors =
        new Dictionary<string, List<DataReflectorText>>();

    public static int GetAmount(string id)
    {
        id = id.ToLowerInvariant();
        return values.TryGetValue(id, out int amount) ? amount : 0;
    }

    public static void SetAmount(string id, int value)
    {
        id = id.ToLowerInvariant();
        values[id] = value;
        NotifyReflectors(id);
    }

    public static void AddAmount(string id, int amount)
    {
        id = id.ToLowerInvariant();
        if (values.ContainsKey(id)) values[id] += amount;
        else values[id] = amount;
        NotifyReflectors(id);
    }

    public static bool TryAddAmount(string id, int amount)
    {
        id = id.ToLowerInvariant();
        bool created = false;
        if (!values.ContainsKey(id))
        {
            values.Add(id, amount);
            created = true;
        }
        else
        {
            values[id] += amount;
        }

        NotifyReflectors(id);
        return created;
    }

    public static void AddAll(Dictionary<string, int> items)
    {
        foreach (KeyValuePair<string, int> kv in items)
        {
            TryAddAmount(kv.Key, kv.Value);
        }
    }

    public static void AddDataReflector(DataReflectorText drt)
    {
        string id = drt.item_id;
        if (!reflectors.TryGetValue(id, out var list))
        {
            list = new List<DataReflectorText>();
            reflectors.Add(id, list);
        }
        list.Add(drt);

        drt.UpdateText(GetAmount(id));
    }

    public static void RemoveDataReflector(DataReflectorText drt)
    {
        string id = drt.item_id;
        if (!reflectors.TryGetValue(id, out var list)) return;
        list.Remove(drt);
        if (list.Count == 0) reflectors.Remove(id);
    }

    private static void NotifyReflectors(string id)
    {
        id = id.ToLowerInvariant();
        if (!reflectors.TryGetValue(id, out var list)) return;
        int current = GetAmount(id);
        for (int i = 0; i < list.Count; i++)
            list[i].UpdateText(current);
    }
}


// should always reflect the current value in player data.
public class DataReflectorText
{
    public readonly string item_id;
    private readonly TextMeshProUGUI textUi;
    private readonly string template;
    private static readonly Regex AmountRegex = new Regex(@"\{amount(?::([^}]+))?\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public DataReflectorText(string id, TextMeshProUGUI ui, string templateOverride = null)
    {
        item_id = id.ToLowerInvariant();
        textUi = ui;
        template = string.IsNullOrEmpty(templateOverride) && textUi != null ? textUi.text : templateOverride;
        if (string.IsNullOrEmpty(template)) template = "{amount}";
    }

    public void UpdateText(int amount)
    {
        if (textUi == null) return;

        // Replace all {amount[:format]} tokens with the formatted value
        string result = AmountRegex.Replace(template, match =>
        {
            var fmtGroup = match.Groups[1];
            if (fmtGroup.Success)
            {
                try { return amount.ToString(fmtGroup.Value); }
                catch { return amount.ToString(); }
            }
            return amount.ToString();
        });

        textUi.text = result;
    }
}