using UnityEngine;

namespace CombatSystem.UnitSystem
{
    [CreateAssetMenu(fileName = "NewParty", menuName =  "ScriptableObjects/Party", order = 0)]
    public class PartySO : ScriptableObject
    {
        [Tooltip("Each party can only have 3 Units max.")]
        public Unit[] partyMembers = new Unit[3];
    }
}