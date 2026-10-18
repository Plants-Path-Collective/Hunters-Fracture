using System;
using Core.CombatSystem.Units;
using Core.CombatSystem.ItemSystem;
using Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Core.UI;

public class InspectionPanel : MonoBehaviour
{
    public event Action Hidden;

    // References
    public UnitDefinitionSO unitInspected;
    [SerializeField] private ItemCatalog itemCatalog;

    [Header("--- Identity ---")]
    public TextMeshProUGUI nameText;
    public Image portraitImage;
    public TextMeshProUGUI tag1Text;
    public TextMeshProUGUI tag2Text;
    public TextMeshProUGUI tag3Text; 
    
    [Header("--- Combat Info ---")]
    public TextMeshProUGUI combatSlotText;
    public TextMeshProUGUI unitTypeText;

    [Header("--- Stats ---")]
    public TextMeshProUGUI HPText;
    public TextMeshProUGUI SPText;
    public TextMeshProUGUI SpeedText;
    public TextMeshProUGUI StrengthText;
    public TextMeshProUGUI MagicPowerText;
    public TextMeshProUGUI PDefText;
    public TextMeshProUGUI MDefText;

    [Header("--- Items Inventory ---")]
    public GameObject itemPrefab;
    public Transform inventoryContainer;

    private readonly List<GameObject> spawnedItemViews = new();

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Show(PartyMemberData member, COMBAT_SLOT slot)
    {
        if (member == null || member.definition == null)
        {
            Debug.LogError($"[{nameof(InspectionPanel)}] Cannot inspect a null party member or definition.", this);
            return;
        }

        if (!UnitStatsController.TryCalculateItemAdjustedStats(
                member.definition, member.inventorySnapshot, itemCatalog, out UnitStatValues adjustedStats))
            return;

        unitInspected = member.definition;
        if (nameText != null) nameText.text = member.definition.unitName;
        if (portraitImage != null)
        {
            portraitImage.sprite = member.definition.partyPortrait;
            portraitImage.enabled = member.definition.partyPortrait != null;
        }
        if (combatSlotText != null) combatSlotText.text = slot.ToString();
        if (unitTypeText != null) unitTypeText.text = member.definition.type.ToString();
        if (tag1Text != null) tag1Text.text = member.definition.tag1;
        if (tag2Text != null) tag2Text.text = member.definition.tag2;
        if (tag3Text != null) tag3Text.text = member.definition.tag3;

        UnitStatValues baseStats = UnitStatValues.From(member.definition);
        SetStat(HPText, baseStats.HP, adjustedStats.HP, true);
        SetStat(SPText, baseStats.SP, adjustedStats.SP, true);
        SetStat(SpeedText, baseStats.Speed, adjustedStats.Speed);
        SetStat(StrengthText, baseStats.Strength, adjustedStats.Strength);
        SetStat(MagicPowerText, baseStats.MagicPower, adjustedStats.MagicPower);
        SetStat(PDefText, baseStats.PhysicalDefense, adjustedStats.PhysicalDefense, percentage: true);
        SetStat(MDefText, baseStats.MagicalDefense, adjustedStats.MagicalDefense, percentage: true);

        PopulateInventory(member.inventorySnapshot);
        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);

    private void OnDisable()
    {
        Hidden?.Invoke();
    }

    private void PopulateInventory(ItemSnapshot snapshot)
    {
        ClearInventory();

        if (snapshot == null || snapshot.Count == 0)
            return;

        bool hasItems = false;
        foreach (KeyValuePair<int, int> entry in snapshot)
        {
            if (entry.Value <= 0) continue;
            hasItems = true;
            break;
        }

        if (!hasItems)
            return;

        if (inventoryContainer == null)
        {
            Debug.LogError($"[{nameof(InspectionPanel)}] Inventory container is not assigned.", this);
            return;
        }

        if (itemPrefab == null)
        {
            Debug.LogError($"[{nameof(InspectionPanel)}] Reward item prefab is not assigned.", this);
            return;
        }

        if (itemCatalog == null)
        {
            Debug.LogError($"[{nameof(InspectionPanel)}] Item catalog is not assigned.", this);
            return;
        }

        if (!itemPrefab.TryGetComponent(out RewardItemView prefabView))
        {
            Debug.LogError($"[{nameof(InspectionPanel)}] Reward item prefab must have a " +
                           $"{nameof(RewardItemView)} component on its root.", itemPrefab);
            return;
        }

        foreach (KeyValuePair<int, int> entry in snapshot)
        {
            if (entry.Value <= 0)
                continue;

            ItemSO item = itemCatalog.GetById(entry.Key);
            if (item == null)
                continue;

            RewardItemView itemView = Instantiate(prefabView, inventoryContainer, false);
            itemView.Bind(item, entry.Value);
            spawnedItemViews.Add(itemView.gameObject);
        }
    }

    private void ClearInventory()
    {
        foreach (GameObject itemView in spawnedItemViews)
        {
            if (itemView != null)
                Destroy(itemView);
        }

        spawnedItemViews.Clear();
    }

    private static void SetStat(
        TextMeshProUGUI label,
        float baseValue,
        float adjustedValue,
        bool wholeNumber = false,
        bool percentage = false)
    {
        if (label == null) return;

        if (wholeNumber)
        {
            baseValue = Mathf.RoundToInt(baseValue);
            adjustedValue = Mathf.RoundToInt(adjustedValue);
        }

        if (percentage)
        {
            baseValue *= 100f;
            adjustedValue *= 100f;
        }

        float bonus = adjustedValue - baseValue;
        string bonusText = bonus >= 0f ? $"+{bonus:0.##}" : $"{bonus:0.##}";
        string suffix = percentage ? "%" : string.Empty;
        label.text = $"{baseValue:0.##}{suffix} ({bonusText}{suffix})";
    }
}
