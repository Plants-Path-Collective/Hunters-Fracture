using UnityEngine;

namespace Core.CombatSystem.Units
{
    [CreateAssetMenu(fileName = "NewUnitDefinition", menuName = "Units/Unit Definition")]
    public class UnitDefinitionSO : ScriptableObject
    {
        [Header("--- Identity ---")]
        public string unitName;
        public UNITY_TYPE type;
        public UNIT_TEAM team;
        public Sprite portrait;
        [TextArea(3, 5)]
        public string description;

        [Header("--- Presentation ---")]
        [Tooltip("Combat prefab: model, animations and the Unit component with its controllers. Instantiated by CombatController.")]
        public GameObject unitPrefab;

        [Header("--- Stats ---")]
        public int maxHP;
        public int maxSP;
        public float speed;
        public float strength;
        public float magicPower;
        public float physicalDefense;
        public float magicalDefense;

        [Header("--- Combat Parameters ---")]
        public float SPRegenMin;
        public float SPRegenMax;

        [Tooltip("Multiplier applied to the matching defense stat (Physical/Magical) while the Unit is Defending.")]
        [Min(1f)]
        public float defendMultiplier = 1.5f;

        /// <summary>Rolls a random SP regen amount within [SPRegenMin, SPRegenMax] — Atacar and
        /// Defender share this profile (combat.html).</summary>
        public int RollSPRegen() =>
            Random.Range(Mathf.RoundToInt(SPRegenMin), Mathf.RoundToInt(SPRegenMax) + 1);

        private void OnValidate()
        {
            if (unitPrefab != null && unitPrefab.GetComponent<Unit>() == null)
                Debug.LogWarning($"[UnitDefinitionSO] '{name}': unitPrefab has no Unit component.", this);
        }
    }
}