using System.Collections.Generic;

namespace SimpleJRPG
{
    public struct DamageEvent
    {
        public ICombatant Source;
        public ICombatant Target;
        public int Amount;
        public string DamageType;
        public string Element;
        public bool WasCrit;
    }

    public struct HealEvent
    {
        public ICombatant Source;
        public ICombatant Target;
        public int Amount;
    }

    public struct StatusEvent
    {
        public ICombatant Source;
        public ICombatant Target;
        public string StatusId;
        public int Duration;
    }

    public struct BuffEvent
    {
        public ICombatant Source;
        public ICombatant Target;
        public string BuffId;
        public float Amount;
        public int Duration;
    }

    public struct KOEvent
    {
        public ICombatant Target;
        public ICombatant Killer;
    }

    public struct TurnEvent
    {
        public ICombatant Actor;
        public int TurnNumber;
    }

    // --- Before-Events (classes — subscribers can mutate) ---

    public class BeforeDamageEvent
    {
        public ICombatant Source;
        public ICombatant Target;
        public int Amount;
        public string DamageType;
        public string Element;
        public bool IsCrit;
        public bool Cancel;
    }

    public class BeforeHealEvent
    {
        public ICombatant Source;
        public ICombatant Target;
        public int Amount;
        public bool Cancel;
    }

    // --- Group Events ---

    public struct GroupDamageEvent
    {
        public ICombatant Source;
        public IReadOnlyList<DamageEvent> Hits;
        public int TotalDamage;
    }

    public struct GroupHealEvent
    {
        public ICombatant Source;
        public IReadOnlyList<HealEvent> Heals;
        public int TotalHealed;
    }

    public struct GroupStatusEvent
    {
        public ICombatant Source;
        public IReadOnlyList<StatusEvent> Applications;
    }

    public struct GroupBuffEvent
    {
        public ICombatant Source;
        public IReadOnlyList<BuffEvent> Applications;
    }

    // --- History ---

    public struct BattleHistoryEntry
    {
        public int Sequence;
        public int TurnNumber;
        public string EventType;
        public object Event;
    }

    // --- Result ---

    public struct BattleResult
    {
        public BattleState Outcome;
        public int TotalTurns;
        public IReadOnlyList<ICombatant> Survivors;
        public IReadOnlyList<BattleHistoryEntry> History;
    }
}
