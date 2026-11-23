using System.Collections.Generic;
using UnityEngine;
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

        // The asset is shared: units that are playing their repeated turn live in a set keyed by Unit.
        private readonly HashSet<Unit> onRepeatedTurn = new();

        protected override void OnOwnerAfterAction(Unit owner, ActionContext context)
        {
            CombatController combat = owner.Combat;
            if (combat == null || !combat.IsCombatActive || !owner.IsAlive) return;

            // This action was the repeated turn: no roll.
            if (onRepeatedTurn.Remove(owner)) return;

            float roll = Mathf.Clamp01(chance + perStackChance * (GetStacks(owner) - 1));
            if (Random.value >= roll) return;

            if (combat.GrantExtraTurn(owner))
                onRepeatedTurn.Add(owner);
        }

        protected override void OnDetached(Unit unit) => onRepeatedTurn.Remove(unit);
    }
}