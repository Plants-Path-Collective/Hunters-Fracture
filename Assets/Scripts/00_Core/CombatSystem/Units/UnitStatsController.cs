using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Core.CombatSystem.ItemSystem;
using Core.CombatSystem.StatusSystem;

namespace Core.CombatSystem.Units
{
    /// <summary>
    /// Owns the 7 derived stats of a Unit (MaxHP, MaxSP, Strength, MagicPower, PhysicalDefense,
    /// MagicalDefense, Speed). Base values come from a UnitDefinitionSO via SetBase(); "current"
    /// values are always a full recompute from base + active item modifiers — never incremental
    /// deltas, so stats never drift. Unit only reads these through its own pass-through
    /// properties; nothing outside this class should write MaxHP/Strength/etc. directly.
    /// </summary>
    public class UnitStatsController : MonoBehaviour
    {

        // ----- Base (from definition, untouched at runtime) -----
        private int baseMaxHP;
        private int baseMaxSP;
        private float baseSpeed;
        private float baseStrength;
        private float baseMagicPower;
        private float basePhysicalDefense;
        private float baseMagicalDefense;

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
        /// Invoked at the end of every RecalculateStats() call, in case UI or other systems
        /// need to refresh after stats change.
        /// </summary>
        public UnityEvent OnStatsRecalculated { get; private set; } = new UnityEvent();

        // Temporary changes owned by active statuses (see StatusBuffTracker). Kept apart from items:
        // they are replaced as a whole every time the tracker changes.
        private readonly List<TemporaryStatModifier> temporaryModifiers = new();

        private UnitInventory inventory;

        private void Awake()
        {
            // Gives "current" a valid value even if something reads a stat before
            // Unit.InitializeFromDefinition() explicitly calls SetBase()/RecalculateStats().
            ResetToBase();

            inventory = GetComponent<UnitInventory>();

            // Temporary shortcut, kept on purpose per team decision: ideally UnitEffectController
            // is the one listening to OnStackChanged (attach/detach TriggeredEffectSO AND ask for
            // a recalculation in the same callback, per items.html's diagram) — but nothing stops
            // UnitStatsController from also subscribing independently; they're two separate
            // listeners on the same event, not a single shared callback.
            if (inventory != null)
                inventory.OnStackChanged.AddListener(() => RecalculateStats(inventory));
        }

        /// <summary>
        /// Copies the base stats from a UnitDefinitionSO and resets "current" to match.
        /// Call RecalculateStats() right after if there's an inventory to apply
        /// (Unit.InitializeFromDefinition does both in sequence).
        /// </summary>
        public void SetBase(UnitDefinitionSO definition)
        {
            baseMaxHP = definition.maxHP;
            baseMaxSP = definition.maxSP;
            baseSpeed = definition.speed;
            baseStrength = definition.strength;
            baseMagicPower = definition.magicPower;
            basePhysicalDefense = definition.physicalDefense;
            baseMagicalDefense = definition.magicalDefense;

            ResetToBase();
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
        /// Recomputes every "current" stat from scratch: resets to base, then re-applies every
        /// active item modifier. Always a full recompute, never an incremental add/remove.
        /// </summary>
        /// <param name="inventory">This same Unit's UnitInventory — used to find which items
        /// are currently held (quantity greater than 0) so their modifiers can be applied. Null
        /// is safe (e.g. no ItemCatalog assigned yet in a test context) — stats just stay at
        /// base.</param>
        public void RecalculateStats(UnitInventory inventory)
        {
            ResetToBase();
            UpdateStatsDebugText();

            if (inventory != null && inventory.inventory != null)
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

                // Second pass: effects that scale with another stat (they read the stats after all flat/percent items).
                foreach (Item item in inventory.inventory.Values)
                {
                    if (item == null || item.itemSO == null || item.quantity <= 0 || item.itemSO.effects == null)
                        continue;

                    foreach (ItemEffectSO effect in item.itemSO.effects)
                        if (effect is StatScalingEffectSO scaling)
                            ApplyScaling(scaling, item.quantity);
                }
            }

            ApplyTemporaryModifiers();  
            
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

        private void ApplyScaling(StatScalingEffectSO scaling, int stackCount)
        {
            float bonus = scaling.Evaluate(GetCurrentStatValue(scaling.sourceStat), stackCount);
            if (Mathf.Approximately(bonus, 0f)) return;

            switch (scaling.targetStat)
            {
                case STAT_TYPE.HP:              currentMaxHP += Mathf.RoundToInt(bonus); break;
                case STAT_TYPE.SP:              currentMaxSP += Mathf.RoundToInt(bonus); break;
                case STAT_TYPE.Speed:           currentSpeed += bonus; break;
                case STAT_TYPE.Strength:        currentStrength += bonus; break;
                case STAT_TYPE.MagicPower:      currentMagicPower += bonus; break;
                case STAT_TYPE.PhysicalDefense: currentPhysicalDefense += bonus; break;
                case STAT_TYPE.MagicalDefense:  currentMagicalDefense += bonus; break;
            }
        }

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

        /// <summary>Replaces this unit's status modifiers and recomputes the stats from scratch.</summary>
        public void SetTemporaryModifiers(IReadOnlyList<TemporaryStatModifier> modifiers)
        {
            temporaryModifiers.Clear();
            for (int i = 0; i < modifiers.Count; i++)
                temporaryModifiers.Add(modifiers[i]);

            RecalculateStats(inventory);
        }

        /// <summary>Statuses add up per stat (+0.8 and -0.3 → ×1.5), apply after every item modifier,
        /// and never take a stat below 0.</summary>
        private void ApplyTemporaryModifiers()
        {
            if (temporaryModifiers.Count == 0) return;

            foreach (STAT_TYPE stat in System.Enum.GetValues(typeof(STAT_TYPE)))
            {
                float sum = 0f;
                foreach (TemporaryStatModifier modifier in temporaryModifiers)
                    if (modifier.stat == stat) sum += modifier.fraction;

                if (Mathf.Approximately(sum, 0f)) continue;

                float factor = Mathf.Max(0f, 1f + sum);

                switch (stat)
                {
                    case STAT_TYPE.HP:              currentMaxHP = Mathf.RoundToInt(currentMaxHP * factor); break;
                    case STAT_TYPE.SP:              currentMaxSP = Mathf.RoundToInt(currentMaxSP * factor); break;
                    case STAT_TYPE.Speed:           currentSpeed *= factor; break;
                    case STAT_TYPE.Strength:        currentStrength *= factor; break;
                    case STAT_TYPE.MagicPower:      currentMagicPower *= factor; break;
                    case STAT_TYPE.PhysicalDefense: currentPhysicalDefense *= factor; break;
                    case STAT_TYPE.MagicalDefense:  currentMagicalDefense *= factor; break;
                }
            }
        }
    }
}