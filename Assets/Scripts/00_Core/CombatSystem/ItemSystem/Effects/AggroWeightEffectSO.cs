using UnityEngine;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.ItemSystem
{
    /// <summary>
    /// Item 008 (Out of Circulation Bill), part 2: makes the owner more likely to be picked as a
    /// target. Adds to Unit.AggroWeight (base 1); CombatEnemyAI picks targets by that weight.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAggroWeight", menuName = "Item System/Effects/Types/Aggro Weight")]
    public class AggroWeightEffectSO : TriggeredEffectSO
    {
        [Tooltip("Weight added with a single copy. Base weight is 1, so 0.25 = 25% more likely than a plain unit. PLACEHOLDER value.")]
        [SerializeField, Min(0f)] private float aggroBonus = 0.25f;
        [Tooltip("Extra weight per additional copy. PLACEHOLDER value.")]
        [SerializeField, Min(0f)] private float perStackAggroBonus = 0.10f;

        public override void Attach(Unit unit)
        {
            if (unit == null) return;
            unit.SetAggroBonus(this, aggroBonus + perStackAggroBonus * (GetStacks(unit) - 1));
        }

        public override void Detach(Unit unit)
        {
            if (unit == null) return;
            unit.ClearAggroBonus(this);
        }
    }
}