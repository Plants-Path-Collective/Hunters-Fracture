using System;
using Core;
using UnityEngine;

namespace CombatSystem.UnitSystem
{
    /// <summary>
    /// Runtime representation of a combatant.
    /// Initialized from a CharacterSheetSO by UnitSpawner.
    /// Owns its stats, ATB progress, and status condition.
    /// Fires events that the UI and CombatManager listen to.
    /// </summary>
    public class Unit : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────
        public event Action<float, float> OnHPChanged;    // (currentHP, maxHP)
        public event Action<float, float> OnSPChanged;    // (currentSP, maxSP)
        public event Action<float>        OnATBChanged;   // (0–1 normalized progress)
        public event Action<Unit>         OnDeath;
        public event Action<STATUS_CONDITION> OnStatusChanged;

        // ── Sheet reference ───────────────────────────────────────────────────
        public CharacterSheetSO Sheet { get; private set; }

        // ── Identity ──────────────────────────────────────────────────────────
        public UNIT_TYPE UnitType  { get; private set; }
        public string    UnitName  { get; private set; }
        public UNIT_STATE State    { get; private set; } = UNIT_STATE.Alive;

        // ── ATB ───────────────────────────────────────────────────────────────
        // atbPoints is driven externally by ATBController.
        // Exposed as property so ATBController can read/write it.
        public float ATBPoints { get; set; }

        // ── Base stats (set once from sheet) ─────────────────────────────────
        public float MaxHP           { get; private set; }
        public float MaxSP           { get; private set; }
        public float BaseSpeed       { get; private set; }
        public float BaseStrength    { get; private set; }
        public float BaseMagicPower  { get; private set; }
        public float BaseEvasion     { get; private set; }
        public float BaseAccuracy    { get; private set; }
        public float BasePhysicalDef { get; private set; }
        public float BaseMagicalDef  { get; private set; }

        // ── Current stats (modified by buffs/debuffs at runtime) ──────────────
        public float CurrentHP { get; private set; }
        public float CurrentSP { get; private set; }

        // Stat modifiers — additive flat bonuses (positive or negative)
        // ActionResolver adds to these; they reset at end of combat
        public float SpeedMod       { get; set; }
        public float StrengthMod    { get; set; }
        public float MagicPowerMod  { get; set; }
        public float EvasionMod     { get; set; }
        public float AccuracyMod    { get; set; }
        public float PhysicalDefMod { get; set; }
        public float MagicalDefMod  { get; set; }

        // Effective (base + modifier) — used by ActionResolver
        public float Speed       => BaseSpeed       + SpeedMod;
        public float Strength    => BaseStrength    + StrengthMod;
        public float MagicPower  => BaseMagicPower  + MagicPowerMod;
        public float Evasion     => Mathf.Clamp(BaseEvasion  + EvasionMod,  0f, 100f);
        public float Accuracy    => Mathf.Clamp(BaseAccuracy + AccuracyMod, 0f, 100f);
        public float PhysicalDef => Mathf.Clamp(BasePhysicalDef + PhysicalDefMod, 0f, 100f);
        public float MagicalDef  => Mathf.Clamp(BaseMagicalDef  + MagicalDefMod,  0f, 100f);

        // ── Status ────────────────────────────────────────────────────────────
        public STATUS_CONDITION ActiveStatus { get; private set; } = STATUS_CONDITION.None;

        // ── Skill usage tracking ──────────────────────────────────────────────
        // Tracks remaining uses for skills with a usageLimit
        private int[] _skillUsesRemaining;

        // ── Initialization ────────────────────────────────────────────────────

        /// <summary>
        /// Called by UnitSpawner right after instantiating the prefab.
        /// Copies all data from the CharacterSheetSO into runtime properties.
        /// </summary>
        public void InitFromSheet(CharacterSheetSO sheet)
        {
            Sheet = sheet;

            UnitType      = sheet.unitType;
            UnitName      = sheet.characterName;

            MaxHP           = sheet.HP;
            MaxSP           = sheet.SP;
            BaseSpeed       = sheet.speed;
            BaseStrength    = sheet.strenght;
            BaseMagicPower  = sheet.magicPower;
            BaseEvasion     = sheet.evasion;
            BaseAccuracy    = sheet.accuracy;
            BasePhysicalDef = sheet.physicalDefense;
            BaseMagicalDef  = sheet.magicalDefense;

            CurrentHP = MaxHP;
            CurrentSP = MaxSP;
            ATBPoints = 0f;
            State     = UNIT_STATE.Alive;

            // Initialize per-skill usage counters
            if (sheet.skills != null)
            {
                _skillUsesRemaining = new int[sheet.skills.Length];
                for (int i = 0; i < sheet.skills.Length; i++)
                    _skillUsesRemaining[i] = sheet.skills[i].usageLimit; // -1 = unlimited
            }
        }

        // ── HP / SP Mutation ─────────────────────────────────────────────────

        public void ModifyHP(float delta)
        {
            if (State == UNIT_STATE.Dead) return;

            CurrentHP = Mathf.Clamp(CurrentHP + delta, 0f, MaxHP);
            OnHPChanged?.Invoke(CurrentHP, MaxHP);

            if (CurrentHP <= 0f)
                Die();
        }

        public void ModifySP(float delta)
        {
            CurrentSP = Mathf.Clamp(CurrentSP + delta, 0f, MaxSP);
            OnSPChanged?.Invoke(CurrentSP, MaxSP);
        }

        // ── Status ────────────────────────────────────────────────────────────

        public void ApplyStatus(STATUS_CONDITION condition)
        {
            ActiveStatus = condition;
            OnStatusChanged?.Invoke(ActiveStatus);
        }

        public void ClearStatus()
        {
            ActiveStatus = STATUS_CONDITION.None;
            OnStatusChanged?.Invoke(ActiveStatus);
        }

        // ── Skill usage ───────────────────────────────────────────────────────

        public bool CanUseSkill(int skillIndex)
        {
            if (_skillUsesRemaining == null || skillIndex >= _skillUsesRemaining.Length)
                return false;

            int remaining = _skillUsesRemaining[skillIndex];
            return remaining == -1 || remaining > 0; // -1 = unlimited
        }

        public void ConsumeSkillUse(int skillIndex)
        {
            if (_skillUsesRemaining == null || skillIndex >= _skillUsesRemaining.Length) return;
            if (_skillUsesRemaining[skillIndex] > 0)
                _skillUsesRemaining[skillIndex]--;
        }

        // ── ATB ───────────────────────────────────────────────────────────────

        /// <summary>Notifies UI listeners of current ATB fill (0–1).</summary>
        public void BroadcastATBProgress(float threshold)
        {
            OnATBChanged?.Invoke(Mathf.Clamp01(ATBPoints / threshold));
        }

        /// <summary>Resets ATB to zero (called after acting or losing a Disputa).</summary>
        public void ResetATB()
        {
            ATBPoints = 0f;
            OnATBChanged?.Invoke(0f);
        }

        // ── Death ─────────────────────────────────────────────────────────────

        private void Die()
        {
            State = UNIT_STATE.Dead;
            ResetATB();
            OnDeath?.Invoke(this);
        }

        /// <summary>Restores the unit from Dead state (Revive effect).</summary>
        public void Revive(float hpPercent)
        {
            State     = UNIT_STATE.Alive;
            CurrentHP = Mathf.Max(1f, MaxHP * Mathf.Clamp01(hpPercent));
            OnHPChanged?.Invoke(CurrentHP, MaxHP);
        }
    }
}
