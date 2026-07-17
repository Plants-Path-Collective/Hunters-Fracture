using System.Collections.Generic;

namespace SimpleJRPG
{
    public interface ITurnSystem
    {
        void Init(List<ICombatant> combatants);
        ICombatant GetNextActor();
        IReadOnlyList<ICombatant> GetTimeline();
        void OnActorDied(ICombatant combatant);
        void OnActorAdded(ICombatant combatant);
        void OnTurnEnd();
    }
}
