using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleJRPG
{
    public class Battle
    {
        public BattleState State { get; private set; } = BattleState.NotStarted;
        public int TurnNumber { get; private set; }
        public List<ICombatant> Combatants { get; private set; }
        public ITurnSystem TurnSystem { get; private set; }
        public ICombatant CurrentActor { get; private set; }
        public BattleResult? Result { get; private set; }
        public IReadOnlyList<BattleHistoryEntry> History => _history;

        private readonly List<BattleHistoryEntry> _history = new List<BattleHistoryEntry>();
        private int _historySequence;

        // Events
        public event Action<Battle> OnBattleStart;
        public event Action<Battle, BattleState> OnBattleEnd;
        public event Action<TurnEvent> OnTurnStart;
        public event Action<TurnEvent> OnTurnEnd;
        public event Action<BeforeDamageEvent> OnBeforeDamage;
        public event Action<DamageEvent> OnDamageDealt;
        public event Action<BeforeHealEvent> OnBeforeHeal;
        public event Action<HealEvent> OnHealed;
        public event Action<KOEvent> OnKO;
        public event Action<ICombatant> OnRevived;
        public event Action<StatusEvent> OnStatusApplied;
        public event Action<ICombatant, string> OnStatusRemoved;
        public event Action<BuffEvent> OnBuffApplied;
        public event Action<ICombatant, string> OnBuffRemoved;
        public event Action<ICombatant> OnFled;
        public event Action<ICombatant> OnCombatantRemoved;
        public event Action<GroupDamageEvent> OnGroupDamageDealt;
        public event Action<GroupHealEvent> OnGroupHealed;
        public event Action<GroupStatusEvent> OnGroupStatusApplied;
        public event Action<GroupBuffEvent> OnGroupBuffApplied;

        // --- Lifecycle ---

        public void Start(List<ICombatant> combatants, ITurnSystem turnSystem)
        {
            if (State != BattleState.NotStarted)
                return;

            Combatants = combatants;
            TurnSystem = turnSystem;
            TurnNumber = 0;
            Result = null;

            _history.Clear();
            _historySequence = 0;

            TurnSystem.Init(Combatants);
            State = BattleState.WaitingForCommands;
            OnBattleStart?.Invoke(this);
        }

        public void EndBattle(BattleState result)
        {
            if (State == BattleState.Victory || State == BattleState.Defeat || State == BattleState.Fled)
                return;

            State = result;
            CurrentActor = null;

            Result = new BattleResult
            {
                Outcome = result,
                TotalTurns = TurnNumber,
                Survivors = Combatants.Where(c => c.IsAlive).ToList(),
                History = _history.ToList()
            };

            OnBattleEnd?.Invoke(this, result);
        }

        // --- Turn Flow ---

        public ICombatant BeginNextTurn()
        {
            if (State != BattleState.WaitingForCommands && State != BattleState.Executing)
                return null;

            CurrentActor = TurnSystem.GetNextActor();
            if (CurrentActor == null)
                return null;

            TurnNumber++;
            State = BattleState.Executing;

            var evt = new TurnEvent
            {
                Actor = CurrentActor,
                TurnNumber = TurnNumber
            };
            OnTurnStart?.Invoke(evt);
            RecordHistory("TurnStart", evt);

            return CurrentActor;
        }

        public void EndTurn()
        {
            if (State != BattleState.Executing)
                return;

            var evt = new TurnEvent
            {
                Actor = CurrentActor,
                TurnNumber = TurnNumber
            };
            OnTurnEnd?.Invoke(evt);
            RecordHistory("TurnEnd", evt);

            TurnSystem.OnTurnEnd();
            CurrentActor = null;

            if (!CheckBattleEnd())
                State = BattleState.WaitingForCommands;
        }

        // --- Combat Actions ---

        public void DealDamage(ICombatant source, ICombatant target, int amount,
            string damageType = "", string element = "", bool isCrit = false)
        {
            DealDamageInternal(source, target, amount, damageType, element, isCrit);
        }

        public void DealDamage(ICombatant source, IList<ICombatant> targets, int amount,
            string damageType = "", string element = "", bool isCrit = false)
        {
            var hits = new List<DamageEvent>();
            int total = 0;

            for (int i = 0; i < targets.Count; i++)
            {
                var result = DealDamageInternal(source, targets[i], amount, damageType, element, isCrit);
                if (result != null)
                {
                    hits.Add(result.Value);
                    total += result.Value.Amount;
                }
            }

            var groupEvt = new GroupDamageEvent
            {
                Source = source,
                Hits = hits,
                TotalDamage = total
            };
            OnGroupDamageDealt?.Invoke(groupEvt);
            RecordHistory("GroupDamage", groupEvt);
        }

        private DamageEvent? DealDamageInternal(ICombatant source, ICombatant target, int amount,
            string damageType, string element, bool isCrit)
        {
            var before = new BeforeDamageEvent
            {
                Source = source,
                Target = target,
                Amount = amount,
                DamageType = damageType,
                Element = element,
                IsCrit = isCrit,
                Cancel = false
            };
            OnBeforeDamage?.Invoke(before);

            if (before.Cancel)
                return null;

            target.TakeDamage(before.Amount);

            var evt = new DamageEvent
            {
                Source = source,
                Target = target,
                Amount = before.Amount,
                DamageType = before.DamageType,
                Element = before.Element,
                WasCrit = before.IsCrit
            };
            OnDamageDealt?.Invoke(evt);
            RecordHistory("Damage", evt);

            if (!target.IsAlive)
                HandleKO(target, source);

            return evt;
        }

        public void Heal(ICombatant source, ICombatant target, int amount)
        {
            HealInternal(source, target, amount);
        }

        public void Heal(ICombatant source, IList<ICombatant> targets, int amount)
        {
            var heals = new List<HealEvent>();
            int total = 0;

            for (int i = 0; i < targets.Count; i++)
            {
                var result = HealInternal(source, targets[i], amount);
                if (result != null)
                {
                    heals.Add(result.Value);
                    total += result.Value.Amount;
                }
            }

            var groupEvt = new GroupHealEvent
            {
                Source = source,
                Heals = heals,
                TotalHealed = total
            };
            OnGroupHealed?.Invoke(groupEvt);
            RecordHistory("GroupHeal", groupEvt);
        }

        private HealEvent? HealInternal(ICombatant source, ICombatant target, int amount)
        {
            var before = new BeforeHealEvent
            {
                Source = source,
                Target = target,
                Amount = amount,
                Cancel = false
            };
            OnBeforeHeal?.Invoke(before);

            if (before.Cancel)
                return null;

            target.Heal(before.Amount);

            var evt = new HealEvent
            {
                Source = source,
                Target = target,
                Amount = before.Amount
            };
            OnHealed?.Invoke(evt);
            RecordHistory("Heal", evt);

            return evt;
        }

        public void Kill(ICombatant target, ICombatant killer = null)
        {
            if (!target.IsAlive)
                return;

            target.TakeDamage(int.MaxValue);
            HandleKO(target, killer);
        }

        public void Revive(ICombatant target, int hpAmount)
        {
            if (target.IsAlive)
                return;

            target.Heal(hpAmount);
            TurnSystem.OnActorAdded(target);

            OnRevived?.Invoke(target);
            RecordHistory("Revive", target);
        }

        // --- Status/Buff ---

        public void ApplyStatus(ICombatant source, ICombatant target, string statusId, int duration = -1)
        {
            var evt = new StatusEvent
            {
                Source = source,
                Target = target,
                StatusId = statusId,
                Duration = duration
            };
            OnStatusApplied?.Invoke(evt);
            RecordHistory("Status", evt);
        }

        public void ApplyStatus(ICombatant source, IList<ICombatant> targets, string statusId, int duration = -1)
        {
            var applications = new List<StatusEvent>();

            for (int i = 0; i < targets.Count; i++)
            {
                var evt = new StatusEvent
                {
                    Source = source,
                    Target = targets[i],
                    StatusId = statusId,
                    Duration = duration
                };
                OnStatusApplied?.Invoke(evt);
                RecordHistory("Status", evt);
                applications.Add(evt);
            }

            var groupEvt = new GroupStatusEvent
            {
                Source = source,
                Applications = applications
            };
            OnGroupStatusApplied?.Invoke(groupEvt);
            RecordHistory("GroupStatus", groupEvt);
        }

        public void RemoveStatus(ICombatant target, string statusId)
        {
            OnStatusRemoved?.Invoke(target, statusId);
            RecordHistory("StatusRemoved", new { Target = target, StatusId = statusId });
        }

        public void ApplyBuff(ICombatant source, ICombatant target, string buffId, float amount, int duration = -1)
        {
            var evt = new BuffEvent
            {
                Source = source,
                Target = target,
                BuffId = buffId,
                Amount = amount,
                Duration = duration
            };
            OnBuffApplied?.Invoke(evt);
            RecordHistory("Buff", evt);
        }

        public void ApplyBuff(ICombatant source, IList<ICombatant> targets, string buffId, float amount, int duration = -1)
        {
            var applications = new List<BuffEvent>();

            for (int i = 0; i < targets.Count; i++)
            {
                var evt = new BuffEvent
                {
                    Source = source,
                    Target = targets[i],
                    BuffId = buffId,
                    Amount = amount,
                    Duration = duration
                };
                OnBuffApplied?.Invoke(evt);
                RecordHistory("Buff", evt);
                applications.Add(evt);
            }

            var groupEvt = new GroupBuffEvent
            {
                Source = source,
                Applications = applications
            };
            OnGroupBuffApplied?.Invoke(groupEvt);
            RecordHistory("GroupBuff", groupEvt);
        }

        public void RemoveBuff(ICombatant target, string buffId)
        {
            OnBuffRemoved?.Invoke(target, buffId);
            RecordHistory("BuffRemoved", new { Target = target, BuffId = buffId });
        }

        // --- Utility ---

        public void Flee(ICombatant who)
        {
            OnFled?.Invoke(who);
            RecordHistory("Fled", who);
            EndBattle(BattleState.Fled);
        }

        public void RemoveCombatant(ICombatant combatant)
        {
            if (!Combatants.Contains(combatant))
                return;

            if (combatant.IsAlive)
                Kill(combatant);

            Combatants.Remove(combatant);
            OnCombatantRemoved?.Invoke(combatant);
            RecordHistory("CombatantRemoved", combatant);
        }

        public bool CheckBattleEnd()
        {
            var alive = Combatants.Where(c => c.IsAlive).ToList();

            var teamsAlive = alive.Select(c => c.Team).Distinct().ToList();

            if (teamsAlive.Count <= 1 && Combatants.Select(c => c.Team).Distinct().Count() > 1)
            {
                if (teamsAlive.Count == 0 || teamsAlive[0] != 0)
                {
                    EndBattle(BattleState.Defeat);
                    return true;
                }
                else
                {
                    EndBattle(BattleState.Victory);
                    return true;
                }
            }

            return false;
        }

        // --- Queries ---

        public List<ICombatant> GetAlive(int team = -1)
        {
            return Combatants.Where(c => c.IsAlive && (team == -1 || c.Team == team)).ToList();
        }

        public List<ICombatant> GetDead(int team = -1)
        {
            return Combatants.Where(c => !c.IsAlive && (team == -1 || c.Team == team)).ToList();
        }

        public List<ICombatant> GetEnemies(ICombatant combatant)
        {
            return Combatants.Where(c => c.IsAlive && c.Team != combatant.Team).ToList();
        }

        public List<ICombatant> GetAllies(ICombatant combatant, bool includeSelf = false)
        {
            return Combatants.Where(c => c.IsAlive && c.Team == combatant.Team && (includeSelf || c != combatant)).ToList();
        }

        public IReadOnlyList<ICombatant> GetTimeline()
        {
            return TurnSystem.GetTimeline();
        }

        // --- Internal ---

        private void HandleKO(ICombatant target, ICombatant killer)
        {
            TurnSystem.OnActorDied(target);

            var evt = new KOEvent
            {
                Target = target,
                Killer = killer
            };
            OnKO?.Invoke(evt);
            RecordHistory("KO", evt);

            CheckBattleEnd();
        }

        private void RecordHistory(string eventType, object evt)
        {
            _history.Add(new BattleHistoryEntry
            {
                Sequence = _historySequence++,
                TurnNumber = TurnNumber,
                EventType = eventType,
                Event = evt
            });
        }
    }
}
