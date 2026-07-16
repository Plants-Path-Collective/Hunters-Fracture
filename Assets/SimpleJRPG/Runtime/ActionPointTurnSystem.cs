using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleJRPG
{
    public class ActionPointTurnSystem : ITurnSystem
    {
        private readonly int _defaultAP;
        private readonly int _minAP;
        private readonly int _maxAP;

        private List<ICombatant> _turnOrder = new List<ICombatant>();
        private Dictionary<ICombatant, int> _ap = new Dictionary<ICombatant, int>();
        private int _currentIndex;
        private int _extraActions;

        public int MinAP => _minAP;
        public int MaxAP => _maxAP;

        public ActionPointTurnSystem(int defaultAP = 0, int minAP = -4, int maxAP = 4)
        {
            _defaultAP = defaultAP;
            _minAP = minAP;
            _maxAP = maxAP;
        }

        public void Init(List<ICombatant> combatants)
        {
            _turnOrder = combatants
                .Where(c => c.IsAlive)
                .OrderByDescending(c => c.Speed)
                .ToList();
            _currentIndex = 0;
            _extraActions = 0;

            _ap.Clear();
            foreach (var c in combatants)
                _ap[c] = _defaultAP;

            // Grant round-start +1 AP to all alive combatants
            GrantPassiveAP();
        }

        public ICombatant GetNextActor()
        {
            // If extra actions remain, return the same actor
            if (_extraActions > 0)
            {
                var actor = _turnOrder[_currentIndex];
                if (actor.IsAlive)
                    return actor;

                // Actor died mid-action-chain — clear remaining actions, advance
                _extraActions = 0;
            }

            // Skip dead combatants and those with AP < 0
            while (_currentIndex < _turnOrder.Count)
            {
                var actor = _turnOrder[_currentIndex];
                if (actor.IsAlive && GetAP(actor) >= 0)
                    return actor;
                _currentIndex++;
            }

            // Round over — rebuild, grant passive AP, start fresh
            RebuildRound();
            GrantPassiveAP();

            // Skip dead and negative-AP combatants in the new round
            while (_currentIndex < _turnOrder.Count)
            {
                var actor = _turnOrder[_currentIndex];
                if (actor.IsAlive && GetAP(actor) >= 0)
                    return actor;
                _currentIndex++;
            }

            // Everyone is dead or negative — rebuild again next call
            return null;
        }

        public IReadOnlyList<ICombatant> GetTimeline()
        {
            var remaining = new List<ICombatant>();
            for (int i = _currentIndex; i < _turnOrder.Count; i++)
            {
                if (_turnOrder[i].IsAlive && GetAP(_turnOrder[i]) >= 0)
                    remaining.Add(_turnOrder[i]);
            }
            return remaining;
        }

        public void OnActorDied(ICombatant combatant)
        {
            // Dead actors get skipped naturally in GetNextActor.
            // If current actor dies mid-action-chain, extra actions cleared in GetNextActor.
        }

        public void OnActorAdded(ICombatant combatant)
        {
            _turnOrder.Add(combatant);
            _ap[combatant] = _defaultAP;
        }

        public void OnTurnEnd()
        {
            if (_extraActions > 0)
            {
                _extraActions--;
                return;
            }

            _currentIndex++;

            if (_currentIndex >= _turnOrder.Count)
            {
                RebuildRound();
                GrantPassiveAP();
            }
        }

        // --- AP-specific: Queries ---

        public int GetAP(ICombatant combatant)
        {
            return _ap.TryGetValue(combatant, out int ap) ? ap : _defaultAP;
        }

        // --- AP-specific: Actions (called between BeginNextTurn and EndTurn) ---

        public void SpendAP()
        {
            if (_currentIndex < 0 || _currentIndex >= _turnOrder.Count)
                throw new InvalidOperationException("No active actor.");

            var actor = _turnOrder[_currentIndex];

            if (_ap[actor] <= _minAP)
                throw new InvalidOperationException(
                    $"Cannot spend AP: '{actor.Name}' is already at minimum AP ({_minAP}).");

            _ap[actor]--;
            _extraActions++;
        }

        public void SaveAP()
        {
            if (_currentIndex < 0 || _currentIndex >= _turnOrder.Count)
                throw new InvalidOperationException("No active actor.");

            var actor = _turnOrder[_currentIndex];
            _ap[actor] = Math.Min(_ap[actor] + 1, _maxAP);
        }

        // --- Internals ---

        private void RebuildRound()
        {
            _turnOrder = _turnOrder
                .Where(c => c.IsAlive)
                .OrderByDescending(c => c.Speed)
                .ToList();
            _currentIndex = 0;
            _extraActions = 0;
        }

        private void GrantPassiveAP()
        {
            foreach (var combatant in _turnOrder)
            {
                if (combatant.IsAlive)
                    _ap[combatant] = Math.Min(_ap[combatant] + 1, _maxAP);
            }
        }
    }
}
