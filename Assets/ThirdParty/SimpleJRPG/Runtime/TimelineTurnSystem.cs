using System.Collections.Generic;
using System.Linq;

namespace SimpleJRPG
{
    public class TimelineTurnSystem : ITurnSystem
    {
        private readonly int _defaultActionCost;
        private readonly int _baseTick;

        private Dictionary<ICombatant, int> _ticks = new Dictionary<ICombatant, int>();
        private List<ICombatant> _initOrder = new List<ICombatant>();
        private int _currentTick;
        private int _nextActionCost;
        private ICombatant _currentActor;

        public TimelineTurnSystem(int defaultActionCost = 100, int baseTick = 100)
        {
            _defaultActionCost = defaultActionCost;
            _baseTick = baseTick;
        }

        public void Init(List<ICombatant> combatants)
        {
            _ticks.Clear();
            _initOrder = new List<ICombatant>(combatants);

            for (int i = 0; i < combatants.Count; i++)
            {
                var c = combatants[i];
                int tick = c.Speed > 0 ? _baseTick / (int)c.Speed : _baseTick;
                _ticks[c] = tick;
            }

            _currentTick = 0;
            _nextActionCost = _defaultActionCost;
            _currentActor = null;
        }

        public ICombatant GetNextActor()
        {
            _currentActor = GetLowestTickAlive();
            if (_currentActor != null)
                _currentTick = _ticks[_currentActor];

            _nextActionCost = _defaultActionCost;
            return _currentActor;
        }

        public IReadOnlyList<ICombatant> GetTimeline()
        {
            return _ticks
                .Where(kvp => kvp.Key.IsAlive)
                .OrderBy(kvp => kvp.Value)
                .ThenByDescending(kvp => kvp.Key.Speed)
                .ThenBy(kvp => _initOrder.IndexOf(kvp.Key))
                .Select(kvp => kvp.Key)
                .ToList();
        }

        public void OnActorDied(ICombatant combatant)
        {
            _ticks.Remove(combatant);
        }

        public void OnActorAdded(ICombatant combatant)
        {
            _ticks[combatant] = _currentTick + _baseTick;
            if (!_initOrder.Contains(combatant))
                _initOrder.Add(combatant);
        }

        public void OnTurnEnd()
        {
            if (_currentActor != null && _ticks.ContainsKey(_currentActor))
            {
                _ticks[_currentActor] = _currentTick + _nextActionCost;
            }

            _currentActor = null;
        }

        public void SetActionCost(int cost)
        {
            _nextActionCost = cost;
        }

        public int GetTick(ICombatant combatant)
        {
            return _ticks[combatant];
        }

        private ICombatant GetLowestTickAlive()
        {
            ICombatant best = null;
            int bestTick = int.MaxValue;
            float bestSpeed = float.MinValue;
            int bestOrder = int.MaxValue;

            foreach (var kvp in _ticks)
            {
                if (!kvp.Key.IsAlive) continue;

                int tick = kvp.Value;
                float speed = kvp.Key.Speed;
                int order = _initOrder.IndexOf(kvp.Key);

                if (tick < bestTick
                    || (tick == bestTick && speed > bestSpeed)
                    || (tick == bestTick && speed == bestSpeed && order < bestOrder))
                {
                    best = kvp.Key;
                    bestTick = tick;
                    bestSpeed = speed;
                    bestOrder = order;
                }
            }

            return best;
        }
    }
}
