using System.Collections.Generic;
using UnityEngine;
using Core.CombatSystem.SkillSystem;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.ItemSystem
{
    /// <summary>
    /// Item 009 (Split Shot): the basic attack also hits a second enemy, chosen at random among the
    /// living enemies adjacent (by slot) to the picked one, but every target takes only a share of the
    /// normal damage. With no adjacent enemy the attack stays single-target at full damage.
    /// </summary>
    /// <remarks>
    /// Needs ActionResolver.ResolveAttack to go through OnBeforeAction and honor context.targets and
    /// context.effectScale. Several copies do not add targets: they only raise the damage share.
    /// </remarks>
    [CreateAssetMenu(fileName = "NewSplitShot", menuName = "Item System/Effects/Types/Split Shot")]
    public class SplitShotEffectSO : ActionHookEffectSO
    {
        [Header("----- Damage share per target -----")]
        [Tooltip("Share of the normal damage with a single copy. 0.66 = 66%.")]
        [SerializeField, Range(0f, 1f)] private float damageScale = 0.66f;
        [Tooltip("Extra share per additional copy. 0.10 = +10%.")]
        [SerializeField, Range(0f, 1f)] private float perStackDamageScale = 0.10f;

        private readonly List<Unit> adjacent = new();

        protected override void OnOwnerBeforeAction(Unit owner, ActionContext context)
        {
            if (!IsBasicAttack(context) || context.targets.Count != 1) return;

            Unit primary = context.targets[0];
            if (primary == null || owner.Combat == null) return;

            CombatSlotAdjacency.CollectAdjacent(owner.Combat.Roster, primary, adjacent);
            if (adjacent.Count == 0) return;

            Unit second = adjacent[Random.Range(0, adjacent.Count)];
            context.targets.Add(second);

            float share = Mathf.Clamp01(damageScale + perStackDamageScale * (GetStacks(owner) - 1));
            context.effectScale *= share;
        }
    }
}