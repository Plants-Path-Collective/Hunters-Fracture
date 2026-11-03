using System;
using System.Collections.Generic;
using UnityEngine;
using Core.CombatSystem.ItemSystem;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.SkillSystem
{
    /// <summary>
    /// Fury meter plus alternate form. Fury fills with the damage taken by the unit or its allies;
    /// when full the unit enters its form. While in it, every turn the unit ends drains Fury and the
    /// unit reverts at 0. If damage would kill it while in the form, it reverts instead of dying.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFuryFormPassive", menuName = "Skill System/Passives/Fury Form Passive")]
    public class FuryFormPassiveSO : TriggeredEffectSO
    {
        [Header("--- Fury ---")]
        [SerializeField, Min(1)] private int maxFury = 100;
        [Tooltip("Fury gained per 1% of max HP lost by the unit or any ally (0.5: a hit for 25% gives 12).")]
        [SerializeField, Min(0f)] private float furyPerPercentOfMaxHp = 0.5f;
        [Tooltip("Fury lost each time the unit ends a turn while in its form. 0 disables the passive drain.")]
        [SerializeField, Min(0)] private int drainPerTurn = 10;

        [Header("--- Form ---")]
        [SerializeField] private UnitFormSO form;

        private sealed class Handlers
        {
            public Action<DamageContext> afterDamage;
            public Action<Unit> turnEnd;
            public Action<DeathContext> beforeDeath;
        }

        // The asset is shared by every carrier, so each Unit keeps its own delegates here (see TriggeredEffectSO).
        private readonly Dictionary<Unit, Handlers> attached = new();

        public override void Attach(Unit unit)
        {
            CombatController combat = unit.Combat;
            if (combat == null || form == null || attached.ContainsKey(unit)) return;

            // Attach runs again whenever the inventory changes mid-combat: never reset a running meter.
            if (unit.MaxFury <= 0)
                combat.ConfigureFury(unit, maxFury);

            var handlers = new Handlers();

            handlers.afterDamage = context =>
            {
                Unit victim = context.target;
                if (!unit.IsAlive || victim == null || victim.Team != unit.Team || victim.HP <= 0) return;

                float lostPercent = 100f * context.amount / Mathf.Max(1, victim.MaxHP);
                int gained = Mathf.RoundToInt(lostPercent * furyPerPercentOfMaxHp);
                if (gained <= 0) return;

                combat.AddFury(unit, gained);
                if (unit.ActiveForm == null && unit.Fury >= unit.MaxFury)
                    combat.EnterForm(unit, form);
            };

            handlers.turnEnd = acted =>
            {
                if (acted != unit || unit.ActiveForm == null) return;

                combat.AddFury(unit, -drainPerTurn);
                if (unit.Fury <= 0)
                    combat.ExitForm(unit, FORM_EXIT_REASON.FuryDepleted);
            };

            handlers.beforeDeath = context =>
            {
                if (context.unit != unit || unit.ActiveForm == null) return;

                combat.ExitForm(unit, FORM_EXIT_REASON.Downed);
                context.prevented = true;
            };

            combat.OnAfterDamage += handlers.afterDamage;
            combat.OnTurnEnd += handlers.turnEnd;
            combat.OnBeforeDeath += handlers.beforeDeath;
            attached[unit] = handlers;
        }

        public override void Detach(Unit unit)
        {
            if (!attached.TryGetValue(unit, out Handlers handlers)) return;

            CombatController combat = unit.Combat;
            if (combat != null)
            {
                combat.OnAfterDamage -= handlers.afterDamage;
                combat.OnTurnEnd -= handlers.turnEnd;
                combat.OnBeforeDeath -= handlers.beforeDeath;
            }

            attached.Remove(unit);
        }
    }
}