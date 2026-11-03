using UnityEngine;
using Core.CombatSystem;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.StatusSystem
{
    /// <summary>
    /// Marker status: while the bearer has it, damage aimed at the bearer is taken by the unit that
    /// applied it (the status source) instead. Duration counts the bearer's turns, like any status.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBodyshieldStatus", menuName = "Status System/Bodyshield")]
    public class BodyshieldStatusSO : StatusSO
    {
        // Damage paths whose amount already went through defense mitigation, so it must be
        // recomputed with the guardian's defense. Other sources (status ticks, etc.) pass unchanged.
        private const string AttackVia = "attack";
        private const string SkillVia = "skill";

        public override bool TryRedirectIncomingDamage(
            StatusInstance instance, DamageContext context, CombatController combat)
        {
            Unit guardian = instance.source;
            Unit protectedUnit = instance.target;

            if (guardian == null || !guardian.IsAlive || guardian == protectedUnit) return false;
            if (context.target != protectedUnit) return false;

            if (context.via == AttackVia || context.via == SkillVia)
            {
                bool magical = context.damageType == UNITY_TYPE.Magical;
                float from = magical ? protectedUnit.EffectiveMagicalDefense : protectedUnit.EffectivePhysicalDefense;
                float to = magical ? guardian.EffectiveMagicalDefense : guardian.EffectivePhysicalDefense;

                // amount = base / (1 + defense), so swapping defenses is a ratio.
                float ratio = (1f + Mathf.Max(0f, from)) / (1f + Mathf.Max(0f, to));
                context.amount = Mathf.Max(1, Mathf.RoundToInt(context.amount * ratio));
            }

            context.target = guardian;
            return true;
        }
    }
}