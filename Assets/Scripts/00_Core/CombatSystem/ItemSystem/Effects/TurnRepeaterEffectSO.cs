using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Core.CombatSystem.SkillSystem;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.ItemSystem
{
    /// <summary>
    /// Item 012 (Turn Repeater): after the owner attacks or casts a skill, rolls a chance to act again
    /// right away (GrantExtraTurn puts it at the front of the queue). The repeated turn never rolls, so
    /// the item cannot chain.
    /// </summary>
    /// <remarks>
    /// Needs CombatController.EndTurn not to reinsert a unit that is already in the queue (see patch
    /// notes); otherwise the unit would end up twice in the timeline.
    /// </remarks>
    [CreateAssetMenu(fileName = "NewTurnRepeater", menuName = "Item System/Effects/Types/Turn Repeater")]
    public class TurnRepeaterEffectSO : ActionHookEffectSO
    {
        [Header("----- Trigger -----")]
        [Tooltip("Chance with a single copy. 0.10 = 10%.")]
        [SerializeField, Range(0f, 1f)] private float chance = 0.10f;
        [Tooltip("Extra chance per additional copy. 0.05 = +5%.")]
        [SerializeField, Range(0f, 1f)] private float perStackChance = 0.05f;

        [Header("----- UI -----")]
        [Tooltip("Optional icon shown on the owner from the start to the end of the repeated turn.")]
        [SerializeField] private IndicatorSO indicator;

        // After a proc the owner's turn closes twice: the original one (OnTurnEnd fires before the
        // extra turn starts) and the repeated one. Counting both ends the repeat however the extra
        // turn closes (action, Defend, skipped turn).
        private const int TurnEndsPerRepeat = 2;

        private sealed class TurnHandlers
        {
            public Action<Unit> start;
            public Action<Unit> end;
        }

        // The asset is shared: per-Unit state lives in dictionaries keyed by Unit.
        // Presence in turnEndsLeft = a repeat is pending or running; value = turn ends left.
        private readonly Dictionary<Unit, int> turnEndsLeft = new();
        private readonly Dictionary<Unit, TurnHandlers> turnHandlers = new();

        public override void Attach(Unit unit)
        {
            base.Attach(unit); // detaches any stale handler first (calls our Detach below)
            if (unit == null || unit.Combat == null || turnHandlers.ContainsKey(unit)) return;

            var handlers = new TurnHandlers
            {
                start = acting => OnTurnStarted(unit, acting),
                end = acted => OnTurnEnded(unit, acted)
            };
            turnHandlers[unit] = handlers;
            unit.Combat.OnTurnStart += handlers.start;
            unit.Combat.OnTurnEnd += handlers.end;
        }

        public override void Detach(Unit unit)
        {
            if (unit != null && turnHandlers.TryGetValue(unit, out TurnHandlers handlers))
            {
                if (unit.Combat != null)
                {
                    unit.Combat.OnTurnStart -= handlers.start;
                    unit.Combat.OnTurnEnd -= handlers.end;
                }
                turnHandlers.Remove(unit);
            }

            base.Detach(unit);
        }

        protected override void OnOwnerAfterAction(Unit owner, ActionContext context)
        {
            CombatController combat = owner.Combat;
            if (combat == null || !combat.IsCombatActive || !owner.IsAlive) return;

            // A repeat is already pending or running: no roll. The icon stays until the turn closes.
            if (turnEndsLeft.ContainsKey(owner)) return;

            float roll = Mathf.Clamp01(chance + perStackChance * (GetStacks(owner) - 1));
            if (Random.value >= roll) return;

            if (combat.GrantExtraTurn(owner))
                turnEndsLeft[owner] = TurnEndsPerRepeat;
        }

        // The extra turn begins: now the icon appears.
        private void OnTurnStarted(Unit owner, Unit acting)
        {
            if (acting != owner || !turnEndsLeft.ContainsKey(owner)) return;
            if (indicator != null) owner.SetIndicator(indicator);
        }

        private void OnTurnEnded(Unit owner, Unit acted)
        {
            if (acted != owner || !turnEndsLeft.TryGetValue(owner, out int left)) return;

            left--;
            if (left <= 0 || !owner.IsAlive) EndRepeat(owner);
            else turnEndsLeft[owner] = left;
        }

        private void EndRepeat(Unit owner)
        {
            turnEndsLeft.Remove(owner);
            if (indicator != null) owner.ClearIndicator(indicator);
        }

        protected override void OnDetached(Unit unit) => EndRepeat(unit);
    }
}