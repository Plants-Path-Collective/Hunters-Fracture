using UnityEngine;

namespace CombatSystem.Unit
{
    /// <summary>
    /// Controls the stats of a unit, including health, mana, and other attributes.
    /// </summary>
    /// <remarks>
    /// This class is responsible for managing the unit's stats and providing methods to modify them.
    /// It can be extended to include additional stats or functionality as needed.
    /// </remarks>
    public class UnitStatsController : MonoBehaviour
    {
        [Header("----- Stats -----")]
        [SerializeField] private int maxHP;
        [SerializeField] private int maxSP;
        [SerializeField] private float speed; 
        [SerializeField] private float strength; 
        [SerializeField] private float magicPower; 
        [SerializeField] private float physicalDefense; 
        [SerializeField] private float magicalDefense; 

        public int MaxHP => maxHP;
        public int MaxSP => maxSP;
        public float Speed => speed;
    }  
}
