using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Core;
using CombatUnit = Core.CombatSystem.Unit.Unit;

namespace Core.CombatSystem
{
    /// <summary>
    /// Root of a combat encounter. The single point every Unit mutation goes through — nothing
    /// talks to another Unit directly, and nothing outside this class touches TimelineController
    /// directly either (its verbs are relayed here, its OnTimelineChanged is relayed as our own
    /// event). AwaitInput/AISelect/ActionResolver don't exist yet, so EndTurn() currently has to
    /// be called from outside (see ExtendedDebug.CombatDebugHarness).
    /// </summary>
    [RequireComponent(typeof(TimelineController))]
    public class CombatController : MonoBehaviour
    {
        private TimelineController timeline;
        private List<CombatUnit> roster = new();

        /// <summary>Everyone in this encounter, alive or dead. Distinct from Timeline's queue,
        /// which only tracks turn order for units still waiting their turn.</summary>
        public IReadOnlyList<CombatUnit> Roster => roster;

        public CombatUnit CurrentActor { get; private set; }
        public bool IsCombatActive { get; private set; }

        /// <summary>Read-only pass-through of the Timeline's queue — for UI/debug. Nothing
        /// outside CombatController should ever hold a reference to TimelineController itself.</summary>
        public IReadOnlyList<CombatUnit> UpcomingTurns => timeline.Queue;

        // ── Lifecycle events ─────────────────────────────────────────────────
        public event Action<IReadOnlyList<CombatUnit>> OnCombatStart;
        public event Action<CombatUnit> OnTurnStart;
        public event Action<CombatUnit> OnTurnEnd;
        public event Action<COMBAT_OUTCOME> OnCombatEnd;

        // ── Unit state events ────────────────────────────────────────────────
        public event Action<CombatUnit> OnUnitKilled;
        public event Action<CombatUnit> OnUnitRevived;

        // ── Action outcome events ────────────────────────────────────────────
        public event Action<DamageContext> OnBeforeDamage;
        public event Action<DamageContext> OnAfterDamage;
        public event Action<HealContext> OnBeforeHeal;
        public event Action<HealContext> OnAfterHeal;
        public event Action<FleeContext> OnFleeAttempt;

        /// <summary>Relayed straight from TimelineController — nothing outside needs a
        /// reference to it directly.</summary>
        public event Action<CombatUnit, TIMELINE_OPERATION, int> OnTimelineChanged;

        private void Awake()
        {
            timeline = GetComponent<TimelineController>();
            timeline.OnTimelineChanged += (unit, operation, delta) =>
                OnTimelineChanged?.Invoke(unit, operation, delta);
        }

        // ── Flow ──────────────────────────────────────────────────────────────

        public void StartCombat(IEnumerable<CombatUnit> units, UNIT_TEAM advantageTeam)
        {
            roster = units.ToList();
            IsCombatActive = true;

            timeline.Initialize(roster, advantageTeam);
            OnCombatStart?.Invoke(roster);

            AdvanceToNextTurn();
        }

        private void AdvanceToNextTurn()
        {
            CurrentActor = timeline.Pop();
            if (CurrentActor == null)
            {
                Debug.LogWarning($"[{nameof(CombatController)}] Timeline is empty — nothing to act.");
                return;
            }

            // TODO: clear the Defending status on CurrentActor once StatusBuffTracker exists.
            OnTurnStart?.Invoke(CurrentActor);
        }

        /// <summary>
        /// Closes out the current actor's turn. Called from outside for now — normally this
        /// would only fire once an action has fully resolved through ActionResolver.
        /// </summary>
        public void EndTurn()
        {
            if (!IsCombatActive || CurrentActor == null) return;

            CombatUnit finishedActor = CurrentActor;
            OnTurnEnd?.Invoke(finishedActor);

            // If finishedActor died mid-turn (e.g. reflected damage killed the acting unit),
            // it's already gone from the timeline via Kill()'s Remove call — reinserting it
            // here would put a dead unit back into the queue.
            if (finishedActor.IsAlive)
                timeline.Reinsert(finishedActor);

            CurrentActor = null;

            if (TryResolveOutcome(out COMBAT_OUTCOME outcome))
            {
                EndCombat(outcome);
                return;
            }

            AdvanceToNextTurn();
        }

        private void EndCombat(COMBAT_OUTCOME outcome)
        {
            IsCombatActive = false;
            timeline.Clear();
            OnCombatEnd?.Invoke(outcome);
        }

        private bool TryResolveOutcome(out COMBAT_OUTCOME outcome)
        {
            bool anyAllyAlive  = roster.Any(u => u.Team == UNIT_TEAM.Ally  && u.IsAlive);
            bool anyEnemyAlive = roster.Any(u => u.Team == UNIT_TEAM.Enemy && u.IsAlive);

            if (!anyEnemyAlive && anyAllyAlive) { outcome = COMBAT_OUTCOME.Victory; return true; }
            if (!anyAllyAlive)                  { outcome = COMBAT_OUTCOME.Defeat;  return true; }

            outcome = default;
            return false;
        }

        // ── Core verbs: life & death ─────────────────────────────────────────

        /// <summary>Marks a unit dead, pulls it out of the timeline entirely, notifies.</summary>
        public void Kill(CombatUnit unit)
        {
            if (!IsCombatActive || unit == null || !unit.IsAlive) return;

            unit.HP = 0;
            timeline.Remove(unit);
            OnUnitKilled?.Invoke(unit);

            if (TryResolveOutcome(out COMBAT_OUTCOME outcome))
                EndCombat(outcome);
        }

        /// <summary>Revives a dead unit with the given HP, reinserting it at the back of the queue.</summary>
        public void Revive(CombatUnit unit, int hp)
        {
            if (!IsCombatActive || unit == null || unit.IsAlive) return;

            unit.HP = Mathf.Max(1, hp);
            timeline.Reinsert(unit);
            OnUnitRevived?.Invoke(unit);
        }

        /// <summary>
        /// chance = clamp(0.5 + (avgTeamSpeed − avgOtherTeamSpeed) * 0.02, 0.1, 0.9). On success,
        /// combat ends as Fled. On failure, behaves like any other action — the current actor
        /// just loses this turn via the normal EndTurn() flow.
        /// </summary>
        public void Flee(UNIT_TEAM team)
        {
            if (!IsCombatActive || CurrentActor == null) return;

            bool success = UnityEngine.Random.value < CalculateFleeChance(team);
            OnFleeAttempt?.Invoke(new FleeContext { Team = team, Success = success });

            if (success)
            {
                EndCombat(COMBAT_OUTCOME.Fled);
                return;
            }

            EndTurn();
        }

        private float CalculateFleeChance(UNIT_TEAM team)
        {
            UNIT_TEAM otherTeam = team == UNIT_TEAM.Ally ? UNIT_TEAM.Enemy : UNIT_TEAM.Ally;
            float delta = AverageSpeed(team) - AverageSpeed(otherTeam);
            return Mathf.Clamp(0.5f + delta * 0.02f, 0.1f, 0.9f);
        }

        private float AverageSpeed(UNIT_TEAM team)
        {
            List<CombatUnit> members = roster.Where(u => u.Team == team && u.IsAlive).ToList();
            return members.Count == 0 ? 0f : members.Average(u => u.Speed);
        }

        // ── Action verbs ──────────────────────────────────────────────────────

        /// <summary>
        /// Fires OnBeforeDamage (mutable), applies context.Amount, fires OnAfterDamage. Calls
        /// Kill internally if HP drops to 0 — callers never have to remember to check death.
        /// </summary>
        public void DealDamage(CombatUnit source, CombatUnit target, int amount, UNITY_TYPE damageType, string via)
        {
            if (!IsCombatActive || target == null || !target.IsAlive) return;

            var context = new DamageContext
            {
                Source = source,
                Target = target,
                Amount = amount,
                DamageType = damageType,
                Via = via
            };

            OnBeforeDamage?.Invoke(context);
            target.HP = Mathf.Max(0, target.HP - context.Amount);
            OnAfterDamage?.Invoke(context);

            if (target.HP <= 0)
                Kill(target);
        }

        /// <summary>Fires OnBeforeHeal (mutable), applies context.Amount clamped to MaxHP, fires OnAfterHeal.</summary>
        public void Heal(CombatUnit source, CombatUnit target, int amount, string via)
        {
            if (!IsCombatActive || target == null || !target.IsAlive) return;

            var context = new HealContext { Source = source, Target = target, Amount = amount, Via = via };

            OnBeforeHeal?.Invoke(context);
            target.HP = Mathf.Min(target.MaxHP, target.HP + context.Amount);
            OnAfterHeal?.Invoke(context);
        }

        /// <summary>Clamped to 0 — but per spec, the action fails outright if SP isn't enough,
        /// rather than spending a partial amount. Returns false in that case.</summary>
        public bool SpendSP(CombatUnit unit, int amount)
        {
            if (!IsCombatActive || unit == null) return false;
            if (unit.SP < amount) return false;

            unit.SP -= amount;
            return true;
        }

        public void RestoreSP(CombatUnit unit, int amount)
        {
            if (!IsCombatActive || unit == null) return;
            unit.SP = Mathf.Min(unit.MaxSP, unit.SP + amount);
        }

        // ── Timeline delegation (nothing outside touches TimelineController directly) ───────

        public void Advance(CombatUnit unit, int n) => timeline.Advance(unit, n);
        public void Delay(CombatUnit unit, int n) => timeline.Delay(unit, n);
        public void MoveToFront(CombatUnit unit) => timeline.MoveToFront(unit);
        public void MoveToBack(CombatUnit unit) => timeline.MoveToBack(unit);
        public void Swap(CombatUnit a, CombatUnit b) => timeline.Swap(a, b);
        public void InsertAfter(CombatUnit unit, CombatUnit after) => timeline.InsertAfter(unit, after);
        public bool GrantExtraTurn(CombatUnit unit) => timeline.GrantExtraTurn(unit);
    }
}