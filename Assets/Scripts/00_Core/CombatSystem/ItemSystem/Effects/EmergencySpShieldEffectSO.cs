using System;
using System.Collections.Generic;
using UnityEngine;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.ItemSystem
{
    /// <summary>
    /// Item 005 (Moonstone Necklace): when damage leaves the owner under an HP threshold, it
    /// heals a percentage of its max HP and pays a percentage of its CURRENT SP. Needs SP to
    /// trigger, and works once per combat. Lethal damage cannot be saved: the unit is already
    /// dead when this runs.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEmergencySpShield", menuName = "Item System/Effects/Types/Emergency SP Shield")]
    public class EmergencySpShieldEffectSO : AfterDamageEffectSO
    {
        [Header("----- Trigger -----")]
        [Tooltip("Triggers while HP / MaxHP is below this. 0.30 = 30%.")]
        [SerializeField, Range(0f, 1f)] private float hpThreshold = 0.30f;

        [Header("----- Heal (percent of max HP) -----")]
        [SerializeField, Range(0f, 1f)] private float healPercent = 0.20f;
        [SerializeField, Range(0f, 1f)] private float perStackHealPercent = 0.10f;

        [Header("----- Cost (percent of current SP) -----")]
        [SerializeField, Range(0f, 1f)] private float spCostPercent = 0.50f;
        [SerializeField, Range(0f, 1f)] private float perStackSpCostPercent = 0.10f;

        [Header("----- UI -----")]
        [Tooltip("Shown on the owner as soon as the shield triggers, until the end of its next turn.")]
        [SerializeField] private IndicatorSO indicator;
        [Tooltip("Replaces the icon above once that turn ends, for the rest of the combat (the gray one).")]
        [SerializeField] private IndicatorSO usedIndicator;

        [SerializeField] private bool debugLog;

        // The asset is shared: per-Unit state lives in collections keyed by Unit.
        private readonly HashSet<Unit> used = new();

        private sealed class Watch
        {
            public CombatController combat;
            public bool armed; // the owner's next turn has begun
            public Action<Unit> began;
            public Action<Unit> ended;
        }

        private readonly Dictionary<Unit, Watch> watches = new();

        protected override void OnOwnerDamage(Unit owner, DamageContext context)
        {
            if (context.target != owner || !owner.IsAlive || owner.MaxHP <= 0) return;
            if (used.Contains(owner)) return;
            if (owner.HP / (float)owner.MaxHP >= hpThreshold) return;
            if (owner.SP <= 0) return;

            int extraStacks = GetStacks(owner) - 1;
            float healFraction = Mathf.Clamp01(healPercent + perStackHealPercent * extraStacks);
            float costFraction = Mathf.Clamp01(spCostPercent + perStackSpCostPercent * extraStacks);

            int spCost = Mathf.Max(1, Mathf.RoundToInt(owner.SP * costFraction));
            int healAmount = Mathf.Max(1, Mathf.RoundToInt(owner.MaxHP * healFraction));

            if (!owner.Combat.SpendSP(owner, spCost)) return;

            used.RemoveWhere(u => u == null); // units from past encounters are destroyed
            used.Add(owner);
            owner.Combat.Heal(owner, owner, healAmount, "EmergencySpShield");

            if (indicator != null) owner.SetIndicator(indicator);
            StartWatch(owner);
            Log($"triggered on {owner.Name}; current actor = {owner.Combat.CurrentActor?.Name}");
        }

        // Waits for the owner's next turn to begin (or be skipped) and then to end.
        private void StartWatch(Unit owner)
        {
            StopWatch(owner);

            var watch = new Watch { combat = owner.Combat };
            watch.began = acting =>
            {
                if (acting != owner) return;
                watch.armed = true;
                Log($"{owner.Name}: own turn began, armed");
            };
            watch.ended = acted =>
            {
                if (acted != owner) return;
                Log($"{owner.Name}: own turn ended, armed = {watch.armed}");
                if (!watch.armed) return;

                StopWatch(owner);
                if (indicator != null) owner.ClearIndicator(indicator);
                if (usedIndicator != null) owner.SetIndicator(usedIndicator);
            };

            watches[owner] = watch;
            watch.combat.OnTurnStart += watch.began;
            watch.combat.OnTurnSkipped += watch.began;
            watch.combat.OnTurnEnd += watch.ended;
        }

        private void StopWatch(Unit owner)
        {
            if (!watches.TryGetValue(owner, out Watch watch)) return;

            watch.combat.OnTurnStart -= watch.began;
            watch.combat.OnTurnSkipped -= watch.began;
            watch.combat.OnTurnEnd -= watch.ended;
            watches.Remove(owner);
        }

        private void OnValidate()
        {
            if (indicator != null && indicator == usedIndicator)
                Debug.LogWarning($"[{name}] Indicator and Used Indicator are the same asset: the icon can't change.", this);
            if (indicator != null && usedIndicator != null && indicator.tint == usedIndicator.tint)
                Debug.LogWarning($"[{name}] Both indicators have the same Tint: the swap won't be visible.", this);
            if (indicator != null && indicator.expiresOnOwnTurn)
                Debug.LogWarning($"[{name}] 'Indicator' has Expires On Own Turn on: the generic turn rule removes it before this effect does.", this);
            if (usedIndicator != null && usedIndicator.expiresOnOwnTurn)
                Debug.LogWarning($"[{name}] 'Used Indicator' has Expires On Own Turn on: it would disappear at the owner's next turn.", this);
        }

        private void Log(string message)
        {
            if (debugLog) Debug.Log($"[EmergencySpShield] {message}");
        }
    }
}