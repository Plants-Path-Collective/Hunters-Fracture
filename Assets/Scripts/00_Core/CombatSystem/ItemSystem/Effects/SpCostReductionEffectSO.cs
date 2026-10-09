using UnityEngine;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.ItemSystem
{
    /// <summary>
    /// Item 008 (Out of Circulation Bill), part 1: reduces the SP cost of the owner's skills.
    /// Registers a reduction on the Unit (keyed by this asset) that Unit.GetEffectiveSpCost reads.
    /// </summary>
    /// <remarks>
    /// Attach runs again after every stack change (UnitEffectController reconciles from scratch),
    /// so the value is always fresh and never drifts.
    /// </remarks>
    [CreateAssetMenu(fileName = "NewSpCostReduction", menuName = "Item System/Effects/Types/SP Cost Reduction")]
    public class SpCostReductionEffectSO : TriggeredEffectSO
    {
        [Tooltip("Reduction with a single copy. 0.10 = -10%.")]
        [SerializeField, Range(0f, 1f)] private float reductionPercent = 0.10f;
        [Tooltip("Extra reduction per additional copy. 0.03 = -3%.")]
        [SerializeField, Range(0f, 1f)] private float perStackReductionPercent = 0.03f;

        public override void Attach(Unit unit)
        {
            if (unit == null) return;
            unit.SetSpCostReduction(this, reductionPercent + perStackReductionPercent * (GetStacks(unit) - 1));
        }

        public override void Detach(Unit unit)
        {
            if (unit == null) return;
            unit.ClearSpCostReduction(this);
        }
    }
}