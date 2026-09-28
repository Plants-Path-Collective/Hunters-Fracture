using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Core;
using Core.CombatSystem.Units;

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
        private List<Unit> roster = new();

        /// <summary>Everyone in this encounter, alive or dead. Distinct from Timeline's queue,
        /// which only tracks turn order for units still waiting their turn.</summary>
        public IReadOnlyList<Unit> Roster => roster;

        public Unit CurrentActor { get; private set; }
        public bool IsCombatActive { get; private set; }

        /// <summary>Read-only pass-through of the Timeline's queue — for UI/debug. Nothing
        /// outside CombatController should ever hold a reference to TimelineController itself.</summary>
        public IReadOnlyList<Unit> UpcomingTurns => timeline.Queue;

        // ── Lifecycle events ─────────────────────────────────────────────────
        public event Action<IReadOnlyList<Unit>> OnCombatStart;
        public event Action<Unit> OnTurnStart;
        public event Action<Unit> OnTurnEnd;
        public event Action<COMBAT_OUTCOME> OnCombatEnd;

        // ── Unit state events ────────────────────────────────────────────────
        public event Action<Unit> OnUnitKilled;
        public event Action<Unit> OnUnitRevived;

        // ── Action outcome events ────────────────────────────────────────────
        public event Action<DamageContext> OnBeforeDamage;
        public event Action<DamageContext> OnAfterDamage;
        public event Action<HealContext> OnBeforeHeal;
        public event Action<HealContext> OnAfterHeal;
        public event Action<FleeContext> OnFleeAttempt;

        /// <summary>Relayed straight from TimelineController — nothing outside needs a
        /// reference to it directly.</summary>
        public event Action<Unit, TIMELINE_OPERATION, int> OnTimelineChanged;

        private void Awake()
        {
            timeline = GetComponent<TimelineController>();
            timeline.OnTimelineChanged += (unit, operation, delta) =>
                OnTimelineChanged?.Invoke(unit, operation, delta);
        }

        private void Start()
        {
            // Runs once per CombatStage load. If nothing is pending (e.g. a debug scene that calls
            // StartCombat by hand), this does nothing.
            CombatSetUp setUp = CombatSetUp.Consume();
            if (setUp != null)
                StartFromSetUp(setUp);
        }

        /// <summary>
        /// Builds every Unit from the setup (party members as allies, enemy slots as fresh enemies)
        /// and starts the combat. The setup is not stored — after this call it is no longer needed.
        /// </summary>
        public void StartFromSetUp(CombatSetUp setUp)
        {
            var units = new List<Unit>();

            foreach (PartyMemberData member in setUp.Party.Members)
            {
                if (member == null) continue;

                Unit unit = SpawnUnit(member.definition);
                if (unit == null) continue;

                unit.InitializeFromParty(member);
                units.Add(unit);
            }

            foreach (EnemySlotData slot in setUp.Enemies)
            {
                if (slot == null) continue;

                Unit unit = SpawnUnit(slot.definition);
                if (unit == null) continue;

                unit.InitializeFresh(slot.definition, slot.inventorySnapshot);
                units.Add(unit);
            }

            StartCombat(units, setUp.AdvantageTeam);
        }

        /// <summary>Instantiates the combat prefab of a definition, or returns null (with an error) if it has none.</summary>
        private Unit SpawnUnit(UnitDefinitionSO definition)
        {
            if (definition == null || definition.unitPrefab == null)
            {
                Debug.LogError($"[{nameof(CombatController)}] Definition '{(definition != null ? definition.name : "null")}' has no unitPrefab assigned.");
                return null;
            }

            return Instantiate(definition.unitPrefab, transform).GetComponent<Unit>();
        }

        // ── Flow ──────────────────────────────────────────────────────────────

        public void StartCombat(IEnumerable<Unit> units, UNIT_TEAM advantageTeam)
        {
            roster = units.ToList();
            foreach (Unit unit in roster)
                unit.EnterCombat(this);

            IsCombatActive = true;

            timeline.Initialize(roster, advantageTeam);
            OnCombatStart?.Invoke(roster);

            // Units that arrive with 0 HP (persisted party state) start dead: Kill removes them
            // from the timeline and can end the combat outright if a whole side is already down.
            foreach (Unit unit in roster.Where(u => !u.IsAlive).ToList())
                Kill(unit);

            if (IsCombatActive)
                AdvanceToNextTurn();
        }

        private void AdvanceToNextTurn()
        {
            CurrentActor = timeline.Pop();

            // Defensive: shouldn't happen now that Kill() always removes dead units
            // immediately, but skip over one rather than starting a turn for it if it ever does.
            while (CurrentActor != null && !CurrentActor.IsAlive)
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

            Unit finishedActor = CurrentActor;
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

            // Detach every item effect so nothing stays subscribed to this controller
            foreach (Unit unit in roster)
                unit.ExitCombat();
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

        /// <summary>Marks a unit dead, pulls it out of the timeline entirely, notifies. No
        /// IsAlive guard here on purpose — DealDamage already sets HP to 0 before calling this,
        /// so checking IsAlive here would always reject exactly the calls that need to go
        /// through. Calling Kill() twice on the same already-dead unit is harmless (Remove is a
        /// no-op, TryResolveOutcome is idempotent) beyond a possible duplicate OnUnitKilled.</summary>
        public void Kill(Unit unit)
        {
            if (!IsCombatActive || unit == null) return;

            unit.HP = 0;
            timeline.Remove(unit);
            OnUnitKilled?.Invoke(unit);

            if (TryResolveOutcome(out COMBAT_OUTCOME outcome))
                EndCombat(outcome);
        }

        /// <summary>Revives a dead unit with the given HP, reinserting it at the back of the queue.</summary>
        public void Revive(Unit unit, int hp)
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
            OnFleeAttempt?.Invoke(new FleeContext { team = team, success = success });

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
            List<Unit> members = roster.Where(u => u.Team == team && u.IsAlive).ToList();
            return members.Count == 0 ? 0f : members.Average(u => u.Speed);
        }

        // ── Action verbs ──────────────────────────────────────────────────────

        /// <summary>
        /// Fires OnBeforeDamage (mutable), applies context.Amount, fires OnAfterDamage. Calls
        /// Kill internally if HP drops to 0 — callers never have to remember to check death.
        /// </summary>
        public void DealDamage(Unit source, Unit target, int amount, UNITY_TYPE damageType, string via)
        {
            if (!IsCombatActive || target == null || !target.IsAlive) return;

            var context = new DamageContext
            {
                source = source,
                target = target,
                amount = amount,
                damageType = damageType,
                via = via
            };

            OnBeforeDamage?.Invoke(context);
            target.HP = Mathf.Max(0, target.HP - context.amount);
            OnAfterDamage?.Invoke(context);

            if (target.HP <= 0)
                Kill(target);
        }

        /// <summary>Fires OnBeforeHeal (mutable), applies context.Amount clamped to MaxHP, fires OnAfterHeal.</summary>
        public void Heal(Unit source, Unit target, int amount, string via)
        {
            if (!IsCombatActive || target == null || !target.IsAlive) return;

            var context = new HealContext { source = source, target = target, amount = amount, via = via };

            OnBeforeHeal?.Invoke(context);
            target.HP = Mathf.Min(target.MaxHP, target.HP + context.amount);
            OnAfterHeal?.Invoke(context);
        }

        /// <summary>Clamped to 0 — but per spec, the action fails outright if SP isn't enough,
        /// rather than spending a partial amount. Returns false in that case.</summary>
        public bool SpendSP(Unit unit, int amount)
        {
            if (!IsCombatActive || unit == null) return false;
            if (unit.SP < amount) return false;

            unit.SP -= amount;
            return true;
        }

        public void RestoreSP(Unit unit, int amount)
        {
            if (!IsCombatActive || unit == null) return;
            unit.SP = Mathf.Min(unit.MaxSP, unit.SP + amount);
        }

        // ── Timeline delegation (nothing outside touches TimelineController directly) ───────

        public void Advance(Unit unit, int n) => timeline.Advance(unit, n);
        public void Delay(Unit unit, int n) => timeline.Delay(unit, n);
        public void MoveToFront(Unit unit) => timeline.MoveToFront(unit);
        public void MoveToBack(Unit unit) => timeline.MoveToBack(unit);
        public void Swap(Unit a, Unit b) => timeline.Swap(a, b);
        public void InsertAfter(Unit unit, Unit after) => timeline.InsertAfter(unit, after);
        public bool GrantExtraTurn(Unit unit) => timeline.GrantExtraTurn(unit);
    }
}