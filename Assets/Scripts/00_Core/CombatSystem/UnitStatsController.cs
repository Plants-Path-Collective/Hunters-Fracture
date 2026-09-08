using UnityEngine;
using UnityEngine.Events;
using TMPro;
using CombatSystem.ItemSystem;
using Core;

namespace CombatSystem.Unit
{
    /// <summary>
    /// Controls the stats of a unit, including health, mana, and other attributes.
    /// </summary>
    /// <remarks>
    /// <para>Holds two sets of values: the <c>base*</c> fields (tuned in the Inspector,
    /// never touched at runtime) and the <c>current*</c> fields (base stats plus
    /// whatever item modifiers currently apply). Everything outside this class —
    /// combat, UI, formulas — should only ever read the public properties
    /// (which expose the "current" values), never assume they equal the base ones.</para>
    /// </remarks>
    public class UnitStatsController : MonoBehaviour
    {
        [Header("----- Base Stats -----")]
        [SerializeField] private int baseMaxHP;
        [SerializeField] private int baseMaxSP;
        [SerializeField] private float baseSpeed;
        [SerializeField] private float baseStrength;
        [SerializeField] private float baseMagicPower;
        [SerializeField] private float basePhysicalDefense;
        [SerializeField] private float baseMagicalDefense;

        [Header("----- Debug UI -----")]
        [SerializeField] private TextMeshProUGUI statsDebugText;

        // Cached, final values after applying item modifiers.
        // RecalculateStats() is the only method allowed to write these.
        private int currentMaxHP;
        private int currentMaxSP;
        private float currentSpeed;
        private float currentStrength;
        private float currentMagicPower;
        private float currentPhysicalDefense;
        private float currentMagicalDefense;

        public int MaxHP => currentMaxHP;
        public int MaxSP => currentMaxSP;
        public float Speed => currentSpeed;
        public float Strength => currentStrength;
        public float MagicPower => currentMagicPower;
        public float PhysicalDefense => currentPhysicalDefense;
        public float MagicalDefense => currentMagicalDefense;

        /// <summary>
        /// Invoked at the end of every <see cref="RecalculateStats"/> call, in
        /// case UI or other systems need to refresh after stats change.
        /// </summary>
        public UnityEvent OnStatsRecalculated { get; private set; } = new UnityEvent();

        private UnitInventory inventory;

        private void Awake()
        {
            // Gives "current" a valid value even if something reads a stat
            // before Unit.Awake() explicitly calls RecalculateStats().
            ResetToBase();

            inventory = GetComponent<UnitInventory>();
            inventory.OnStackChanged.AddListener(() => RecalculateStats(inventory));
        }

        private void ResetToBase()
        {
            currentMaxHP = baseMaxHP;
            currentMaxSP = baseMaxSP;
            currentSpeed = baseSpeed;
            currentStrength = baseStrength;
            currentMagicPower = baseMagicPower;
            currentPhysicalDefense = basePhysicalDefense;
            currentMagicalDefense = baseMagicalDefense;
        }

        private void UpdateStatsDebugText()
        {
            if (statsDebugText != null)
                statsDebugText.text = $"[HP] {currentMaxHP}";
        }

        /// <summary>
        /// Recomputes every "current" stat from scratch: resets to base, then
        /// re-applies every active item modifier.
        /// </summary>
        /// <remarks>
        /// <para>This is always a full recompute, never an incremental add/remove.
        /// That matches <see cref="UnitInventory.OnStackChanged"/> not carrying
        /// which item changed or by how much, and it avoids drift bugs that come
        /// from applying/undoing deltas over time instead of recomputing from a
        /// known-good base.</para>
        /// <para><b>Current state:</b> until ItemEffectSO / StatModifierEffectSO
        /// exist, this method only resets to base — it's safe to call now and
        /// will start doing real work once item modifiers are implemented,
        /// without any caller needing to change.</para>
        /// </remarks>
        /// <param name="inventory">
        /// This same Unit's UnitInventory — used to find which items are
        /// currently held (quantity greater than 0) so their modifiers can be applied.
        /// </param>
        public void RecalculateStats(UnitInventory inventory)
        {
            ResetToBase();
            UpdateStatsDebugText();

            if (inventory != null)
            {
                foreach (Item item in inventory.inventory.Values)
                {
                    if (item == null || item.itemSO == null || item.quantity <= 0 || item.itemSO.effects == null)
                        continue;

                    foreach (ItemEffectSO effect in item.itemSO.effects)
                    {
                        if (effect is not StatModifierEffectSO statModifier)
                            continue;

                        ApplyModifier(statModifier, item.quantity, item.itemSO.itemName);
                    }
                }
            }

            OnStatsRecalculated.Invoke();
        }

        private void ApplyModifier(StatModifierEffectSO modifier, int stackCount, string itemName)
        {
            float previousValue = GetCurrentStatValue(modifier.statType);

            switch (modifier.statType)
            {
                case STAT_TYPE.HP:
                    currentMaxHP = Mathf.RoundToInt(modifier.ApplyModifiers(currentMaxHP, stackCount));
                    break;
                case STAT_TYPE.SP:
                    currentMaxSP = Mathf.RoundToInt(modifier.ApplyModifiers(currentMaxSP, stackCount));
                    break;
                case STAT_TYPE.Speed:
                    currentSpeed = modifier.ApplyModifiers(currentSpeed, stackCount);
                    break;
                case STAT_TYPE.Strength:
                    currentStrength = modifier.ApplyModifiers(currentStrength, stackCount);
                    break;
                case STAT_TYPE.MagicPower:
                    currentMagicPower = modifier.ApplyModifiers(currentMagicPower, stackCount);
                    break;
                case STAT_TYPE.PhysicalDefense:
                    currentPhysicalDefense = modifier.ApplyModifiers(currentPhysicalDefense, stackCount);
                    break;
                case STAT_TYPE.MagicalDefense:
                    currentMagicalDefense = modifier.ApplyModifiers(currentMagicalDefense, stackCount);
                    break;
            }

            float currentValue = GetCurrentStatValue(modifier.statType);
            float change = currentValue - previousValue;
            string changeText = change >= 0f ? $"+{change:0.##}" : $"{change:0.##}";

            Debug.Log($"[UnitStatsController] {modifier.statType} current value: {currentValue:0.##}");

            if (modifier.statType == STAT_TYPE.HP && statsDebugText != null)
            {
                statsDebugText.text = $"[HP] {currentMaxHP}\n[Item Pick Up] {itemName} {changeText}HP = {currentMaxHP} HP";
            }
        }

        /// <summary>
        /// Method to get the current value of a stat based on its type. Used for debugging purposes.
        /// </summary>
        /// <param name="statType">The type of stat to retrieve.</param>
        /// <returns>The current value of the specified stat.</returns>
        private float GetCurrentStatValue(STAT_TYPE statType)
        {
            return statType switch
            {
                STAT_TYPE.HP => currentMaxHP,
                STAT_TYPE.SP => currentMaxSP,
                STAT_TYPE.Speed => currentSpeed,
                STAT_TYPE.Strength => currentStrength,
                STAT_TYPE.MagicPower => currentMagicPower,
                STAT_TYPE.PhysicalDefense => currentPhysicalDefense,
                STAT_TYPE.MagicalDefense => currentMagicalDefense,
                _ => 0f
            };
        }
    }
}