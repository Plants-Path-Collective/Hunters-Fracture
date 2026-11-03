using UnityEngine;
using Core.CombatSystem.SkillSystem;

namespace Core.CombatSystem.Units
{
    /// <summary>
    /// An alternate form a unit can enter during combat (e.g. Elowen's werewolf). Pure data: when the
    /// form is entered and left is decided by the passive that references it (FuryFormPassiveSO).
    /// </summary>
    [CreateAssetMenu(fileName = "NewUnitForm", menuName = "Units/Unit Form")]
    public class UnitFormSO : ScriptableObject
    {
        [Header("--- Type ---")]
        [Tooltip("Damage type of the unit's basic attack while in this form.")]
        public UNITY_TYPE unitType = UNITY_TYPE.Physical;

        [Header("--- Stats ---")]
        [Tooltip("Max HP becomes the unit's max SP while in this form, and HP is set to the new maximum.")]
        public bool maxHpFromMaxSp = true;
        [Tooltip("Flat values added to the derived stats after items and before statuses.")]
        public FlatStatBonus[] flatBonuses;

        [Header("--- Kit ---")]
        [Tooltip("Replaces the unit's skills while in this form.")]
        public ActionSO[] skills;
        [Tooltip("Replaces the unit's ultimate while in this form (e.g. 'Return to human').")]
        public ActionSO ultimate;

        [Header("--- Rules ---")]
        [Tooltip("If HP reaches 0 the unit leaves the form instead of dying, keeping the HP it had " +
                 "before the form reduced by this fraction (0.15 = 15% less).")]
        [Range(0f, 1f)] public float downedHpPenalty = 0.15f;
    }
}