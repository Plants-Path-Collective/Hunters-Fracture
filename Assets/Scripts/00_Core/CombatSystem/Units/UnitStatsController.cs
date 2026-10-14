using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Core.CombatSystem.ItemSystem;
using Core.CombatSystem.StatusSystem;

namespace Core.CombatSystem.Units
{
    public struct UnitStatValues
    {
        public float HP;
        public float SP;
        public float Speed;
        public float Strength;
        public float MagicPower;
        public float PhysicalDefense;
        public float MagicalDefense;

        public float Get(STAT_TYPE stat) => stat switch
        {
            STAT_TYPE.HP => HP,
            STAT_TYPE.SP => SP,
            STAT_TYPE.Speed => Speed,
            STAT_TYPE.Strength => Strength,
            STAT_TYPE.MagicPower => MagicPower,
            STAT_TYPE.PhysicalDefense => PhysicalDefense,
            STAT_TYPE.MagicalDefense => MagicalDefense,
            _ => 0f
        };

        public void Set(STAT_TYPE stat, float value)
        {
            switch (stat)
            {
                case STAT_TYPE.HP: HP = value; break;
                case STAT_TYPE.SP: SP = value; break;
                case STAT_TYPE.Speed: Speed = value; break;
                case STAT_TYPE.Strength: Strength = value; break;
                case STAT_TYPE.MagicPower: MagicPower = value; break;
                case STAT_TYPE.PhysicalDefense: PhysicalDefense = value; break;
                case STAT_TYPE.MagicalDefense: MagicalDefense = value; break;
            }
        }

        public static UnitStatValues From(UnitDefinitionSO definition) => new()
        {
            HP = definition.maxHP,
            SP = definition.maxSP,
            Speed = definition.speed,
            Strength = definition.strength,
            MagicPower = definition.magicPower,
            PhysicalDefense = definition.physicalDefense,
            MagicalDefense = definition.magicalDefense
        };
    }

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

        public static bool TryCalculateItemAdjustedStats(
            UnitDefinitionSO definition,
            ItemSnapshot snapshot,
            ItemCatalog catalog,
            out UnitStatValues values)
        {
            if (definition == null)
            {
                Debug.LogError($"[{nameof(UnitStatsController)}] Cannot calculate stats without a unit definition.");
                values = default;
                return false;
            }

            var items = new List<Item>();
            if (snapshot != null)
            {
                bool hasItems = false;
                foreach (KeyValuePair<int, int> entry in snapshot)
                    if (entry.Value > 0) { hasItems = true; break; }

                if (hasItems && catalog == null)
                {
                    Debug.LogError($"[{nameof(UnitStatsController)}] Cannot calculate item-adjusted stats " +
                                   "without an ItemCatalog.");
                    values = default;
                    return false;
                }

                if (catalog != null)
                {
                    foreach (KeyValuePair<int, int> entry in snapshot)
                    {
                        if (entry.Value <= 0) continue;

                        ItemSO itemSO = catalog.GetById(entry.Key);
                        if (itemSO == null)
                        {
                            values = default;
                            return false;
                        }

                        items.Add(new Item { itemSO = itemSO, quantity = entry.Value });
                    }
                }
            }

            values = CalculateItemAdjustedStats(UnitStatValues.From(definition), items);
            return true;
        }

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
            UnitStatValues values = CalculateItemAdjustedStats(
                new UnitStatValues
                {
                    HP = baseMaxHP,
                    SP = baseMaxSP,
                    Speed = baseSpeed,
                    Strength = baseStrength,
                    MagicPower = baseMagicPower,
                    PhysicalDefense = basePhysicalDefense,
                    MagicalDefense = baseMagicalDefense
                },
                inventory != null ? inventory.inventory.Values : null);

            currentMaxHP = Mathf.RoundToInt(values.HP);
            currentMaxSP = Mathf.RoundToInt(values.SP);
            currentSpeed = values.Speed;
            currentStrength = values.Strength;
            currentMagicPower = values.MagicPower;
            currentPhysicalDefense = values.PhysicalDefense;
            currentMagicalDefense = values.MagicalDefense;
            UpdateStatsDebugText();

            ApplyTemporaryModifiers();  
            
            OnStatsRecalculated.Invoke();
        }

        private static UnitStatValues CalculateItemAdjustedStats(UnitStatValues values, IEnumerable<Item> items)
        {
            if (items == null) return values;

            foreach (Item item in items)
            {
                if (item?.itemSO?.effects == null || item.quantity <= 0) continue;

                foreach (ItemEffectSO effect in item.itemSO.effects)
                    if (effect is StatModifierEffectSO modifier)
                        ApplyModifier(ref values, modifier, item.quantity);
            }

            foreach (Item item in items)
            {
                if (item?.itemSO?.effects == null || item.quantity <= 0) continue;

                foreach (ItemEffectSO effect in item.itemSO.effects)
                {
                    if (effect is not StatScalingEffectSO scaling) continue;
                    float bonus = scaling.Evaluate(values.Get(scaling.sourceStat), item.quantity);
                    values.Set(scaling.targetStat, values.Get(scaling.targetStat) + bonus);
                }
            }

            return values;
        }

        private static void ApplyModifier(ref UnitStatValues values, StatModifierEffectSO modifier, int stackCount)
        {
            float value = modifier.ApplyModifiers(values.Get(modifier.statType), stackCount);
            if (modifier.statType == STAT_TYPE.HP || modifier.statType == STAT_TYPE.SP)
                value = Mathf.RoundToInt(value);
            values.Set(modifier.statType, value);
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