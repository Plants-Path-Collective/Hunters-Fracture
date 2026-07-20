using System.Collections.Generic;
using System.Linq;

namespace SimpleJRPG
{
    public class ClassicTurnSystem : ITurnSystem
    {
        private List<ICombatant> _turnOrder = new List<ICombatant>();
        private int _currentIndex;

        public void Init(List<ICombatant> combatants)
        {
            BuildTurnOrder(combatants);
        }

        public ICombatant GetNextActor()
        {
            // Skip dead combatants
            while (_currentIndex < _turnOrder.Count)
            {
                var actor = _turnOrder[_currentIndex];
                if (actor.IsAlive)
                    return actor;
                _currentIndex++;
            }

            // Round over — rebuild and start fresh
            RebuildFromAlive();
            if (_turnOrder.Count == 0)
                return null;

            return _turnOrder[0];
        }

        public IReadOnlyList<ICombatant> GetTimeline()
        {
            // Return remaining actors this round (alive only)
            var remaining = new List<ICombatant>();
            for (int i = _currentIndex; i < _turnOrder.Count; i++)
            {
                if (_turnOrder[i].IsAlive)
                    remaining.Add(_turnOrder[i]);
            }
            return remaining;
        }

        public void OnActorDied(ICombatant combatant)
        {
            // Dead actors get skipped naturally in GetNextActor
        }

        public void OnActorAdded(ICombatant combatant)
        {
            // Mid-round additions go at the end
            _turnOrder.Add(combatant);
        }

        public void OnTurnEnd()
        {
            _currentIndex++;

            // If we've gone through everyone, start a new round
            if (_currentIndex >= _turnOrder.Count)
                RebuildFromAlive();
        }

        private void BuildTurnOrder(List<ICombatant> combatants)
        {
            _turnOrder = combatants
                .Where(c => c.IsAlive)
                .OrderByDescending(c => c.Speed)
                .ToList();
            _currentIndex = 0;
        }

        private void RebuildFromAlive()
        {
            _turnOrder = _turnOrder
                .Where(c => c.IsAlive)
                .OrderByDescending(c => c.Speed)
                .ToList();
            _currentIndex = 0;
        }
    }
}
