using System.Collections.Generic;
using UnityEngine;
using Core.CombatSystem.SkillSystem;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.ItemSystem
{
    /// <summary>
    /// Item 010 (Missfire): when the owner casts an offensive skill, rolls a chance for it to hit EVERY
    /// living enemy and also one living ally adjacent (by slot) to the caster, picked at random.
    /// Center caster: left or right ally. Left/right caster: the center ally.
    /// </summary>
    /// <remarks>
    /// The ally is hit by the same effects as the enemies (friendly fire is the point of the item).
    /// Only skills aimed at enemies are affected; self/ally skills and the basic attack are ignored.
    /// </remarks>
    [CreateAssetMenu(fileName = "NewMissfire", menuName = "Item System/Effects/Types/Missfire")]
    public class MissfireEffectSO : ActionHookEffectSO
    {
        [Header("----- Trigger -----")]
        [Tooltip("Chance with a single copy. 0.20 = 20%.")]
        [SerializeField, Range(0f, 1f)] private float chance = 0.20f;
        [Tooltip("Extra chance per additional copy. 0.20 = +20%.")]
        [SerializeField, Range(0f, 1f)] private float perStackChance = 0.20f;
        [Tooltip("Whether the ultimate can misfire too.")]
        [SerializeField] private bool includeUltimate = false;

        private readonly List<Unit> adjacent = new();

        protected override void OnOwnerBeforeAction(Unit owner, ActionContext context)
        {
            ActionSO action = context.action;
            if (action == null || owner.Combat == null) return;
            if (!includeUltimate && action == owner.Ultimate) return;
            if (action.targetType != TARGET_TYPE.SingleEnemy && action.targetType != TARGET_TYPE.AllEnemies) return;

            float roll = Mathf.Clamp01(chance + perStackChance * (GetStacks(owner) - 1));
            if (Random.value >= roll) return;

            IReadOnlyList<Unit> roster = owner.Combat.Roster;

            context.targets.Clear();
            foreach (Unit unit in roster)
                if (unit != null && unit.IsAlive && unit.Team != owner.Team)
                    context.targets.Add(unit);

            CombatSlotAdjacency.CollectAdjacent(roster, owner, adjacent);
            if (adjacent.Count > 0)
                context.targets.Add(adjacent[Random.Range(0, adjacent.Count)]);
        }
    }
}