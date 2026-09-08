using UnityEngine;

namespace CombatSystem.Unit
{
    public class UnitEffectController : MonoBehaviour
    {
        public void ApplyBuff(string buffName, float value, int durationTurns)
        {
            // Placeholder implementation — replace with the real StatusBuffTracker API
            Debug.Log($"Applying buff '{buffName}' with value {value} for {durationTurns} turns.");
        }
    }
}