using System.Collections.Generic;
using UnityEngine;
using Core.CombatSystem;
using Core.CombatSystem.Units;

namespace Core.CombatSystem.StatusSystem
{
    /// <summary>
    /// One per encounter, owned by CombatController, which is the only caller: statuses are applied
    /// through its ApplyStatus/RemoveStatus verbs. Tracks active statuses per unit, counts their
    /// durations in the BEARER's turns, runs their end-of-turn effects and keeps each unit's
    /// temporary stat modifiers in sync. Nothing persists past the combat.
    /// </summary>
    public class StatusBuffTracker
    {
        private static readonly IReadOnlyList<StatusInstance> None = new List<StatusInstance>();

        private readonly CombatController combat;
        private readonly Dictionary<Unit, List<StatusInstance>> active = new();
        private readonly List<TemporaryStatModifier> scratch = new();

        public StatusBuffTracker(CombatController combat)
        {
            this.combat = combat;

            // Subscribed before any item effect (they attach on EnterCombat), so it runs first.
            combat.OnBeforeDamage += ApplyOutgoingDamageModifiers;
        }

        public IReadOnlyList<StatusInstance> GetStatuses(Unit unit) =>
            active.TryGetValue(unit, out List<StatusInstance> list) ? list : None;

        // ── Verbs (called by CombatController) ───────────────────────────────

        internal StatusInstance Apply(Unit source, Unit target, StatusSO status, float magnitude, int duration)
        {
            if (duration <= 0) return null;

            if (!active.TryGetValue(target, out List<StatusInstance> list))
            {
                list = new List<StatusInstance>();
                active[target] = list;
            }

            StatusInstance instance = list.Find(s => s.definition == status);
            bool refreshed = instance != null;

            if (refreshed)
            {
                // Same status again: refresh, keeping the longer duration and the stronger effect.
                instance.remainingTurns = Mathf.Max(instance.remainingTurns, duration);
                if (magnitude >= instance.magnitude)
                {
                    instance.magnitude = magnitude;
                    instance.source = source;
                }
            }
            else
            {
                instance = new StatusInstance
                {
                    definition = status,
                    source = source,
                    target = target,
                    magnitude = magnitude,
                    remainingTurns = duration
                };
                list.Add(instance);
            }

            SyncStats(target);
            combat.NotifyStatusApplied(ToContext(instance, refreshed));
            return instance;
        }

        internal bool Remove(Unit target, StatusSO status)
        {
            if (!active.TryGetValue(target, out List<StatusInstance> list)) return false;

            StatusInstance instance = list.Find(s => s.definition == status);
            if (instance == null) return false;

            Expire(instance, list);
            return true;
        }

        /// <summary>Death: every status is dropped silently and the unit's stats go back to normal.</summary>
        internal void ClearAll(Unit unit)
        {
            if (!active.Remove(unit)) return;
            SyncStats(unit);
        }

        internal void Clear() => active.Clear();

        // ── Turn hooks ────────────────────────────────────────────────────────

        /// <summary>True if the unit loses this turn. Consumes one skipped turn of its skip status.</summary>
        internal bool ConsumeSkip(Unit unit)
        {
            if (!active.TryGetValue(unit, out List<StatusInstance> list)) return false;

            StatusInstance skip = list.Find(s => s.definition.SkipsTurn);
            if (skip == null) return false;

            skip.remainingTurns--;
            if (skip.remainingTurns <= 0)
                Expire(skip, list);

            return true;
        }

        /// <summary>End-of-turn effects, then the duration countdown. A damage status can kill the
        /// bearer, which clears its list: the copy and the Contains check keep that safe.</summary>
        internal void TickTurnEnd(Unit unit)
        {
            if (!active.TryGetValue(unit, out List<StatusInstance> list)) return;

            foreach (StatusInstance instance in list.ToArray())
            {
                if (!unit.IsAlive) return;
                if (!list.Contains(instance)) continue;

                instance.definition.OnTurnEnd(instance, combat);

                if (!unit.IsAlive) return;
                if (!list.Contains(instance) || !instance.definition.DecrementsOnTurnEnd) continue;

                instance.remainingTurns--;
                if (instance.remainingTurns <= 0)
                    Expire(instance, list);
            }
        }

        // ── Internals ─────────────────────────────────────────────────────────

        private void Expire(StatusInstance instance, List<StatusInstance> list)
        {
            list.Remove(instance);
            if (list.Count == 0)
                active.Remove(instance.target);

            SyncStats(instance.target);
            combat.NotifyStatusExpired(ToContext(instance, false));
        }

        private void SyncStats(Unit unit)
        {
            scratch.Clear();

            if (active.TryGetValue(unit, out List<StatusInstance> list))
                foreach (StatusInstance instance in list)
                    instance.definition.CollectStatModifiers(instance, scratch);

            unit.StatsController.SetTemporaryModifiers(scratch);
        }

        private void ApplyOutgoingDamageModifiers(DamageContext context)
        {
            if (context.source == null || !active.TryGetValue(context.source, out List<StatusInstance> list))
                return;

            float multiplier = 1f;
            foreach (StatusInstance instance in list)
                multiplier *= instance.definition.OutgoingDamageMultiplier(instance);

            if (!Mathf.Approximately(multiplier, 1f))
                context.amount = Mathf.Max(1, Mathf.RoundToInt(context.amount * multiplier));
        }

        private static StatusContext ToContext(StatusInstance instance, bool refreshed) => new()
        {
            source = instance.source,
            target = instance.target,
            status = instance.definition,
            magnitude = instance.magnitude,
            remainingTurns = instance.remainingTurns,
            refreshed = refreshed
        };
    }
}