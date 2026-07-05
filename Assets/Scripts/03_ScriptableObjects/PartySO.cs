using UnityEngine;

namespace CombatSystem.UnitSystem
{
    /// <summary>
    /// Data asset that defines a party's roster.
    /// Holds CharacterSheetSOs. 
    /// Unit instances are created at runtime by UnitSpawner.
    /// </summary>
    [CreateAssetMenu(fileName = "NewParty", menuName = "ScriptableObjects/Party", order = 0)]
    public class PartySO : ScriptableObject
    {
        [Tooltip("Each party can have a maximum of 3 members.")]
        public CharacterSheetSO[] members = new CharacterSheetSO[3];
    }
}