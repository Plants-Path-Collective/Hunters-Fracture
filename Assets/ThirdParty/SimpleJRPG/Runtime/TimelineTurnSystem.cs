using System.Collections.Generic;
using System.Linq;

namespace SimpleJRPG
{
    /// <summary>
    /// Turn-order controller based on a timeline of ticks instead of a simple round robin.
    /// Every actor has a numerical tick value; the actor with the lowest value acts first,
    /// and once they end their turn the system adds their action cost to their current tick.
    /// </summary>
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

        /// <summary>
        /// Initializes the timeline for a new battle. Each combatant receives an initial tick value
        /// based on their speed; faster actors produce lower, earlier ticks and therefore act first.
        /// </summary>
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

        /// <summary>
        /// Selects the alive actor with the lowest tick and marks it as the current actor.
        /// This is the core step of the timeline system: the battle asks for the next eligible actor.
        /// </summary>
        public ICombatant GetNextActor()
        {
            _currentActor = GetLowestTickAlive();
            if (_currentActor != null)
                _currentTick = _ticks[_currentActor];

            _nextActionCost = _defaultActionCost;
            return _currentActor;
        }

        /// <summary>
        /// Returns the battle order sorted from lowest tick to highest tick, which is the visual timeline.
        /// Lower tick = earlier in the timeline; ties are broken by speed and original order.
        /// </summary>
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

        /// <summary>
        /// Removes a combatant from the active tick dictionary when they are defeated.
        /// They no longer participate in future turn selection.
        /// </summary>
        public void OnActorDied(ICombatant combatant)
        {
            _ticks.Remove(combatant);
        }

        /// <summary>
        /// Reintroduces a revived unit into the timeline, placing them after the current tick.
        /// This keeps revived characters from rejoining instantly in the same turn cycle.
        /// </summary>
        public void OnActorAdded(ICombatant combatant)
        {
            _ticks[combatant] = _currentTick + _baseTick;
            if (!_initOrder.Contains(combatant))
                _initOrder.Add(combatant);
        }

        /// <summary>
        /// Advances the actor's tick after their turn ends. The amount added is the action cost,
        /// which may vary per action (fight, defend, heal, flee, etc.).
        /// </summary>
        public void OnTurnEnd()
        {
            if (_currentActor != null && _ticks.ContainsKey(_currentActor))
            {
                _ticks[_currentActor] = _currentTick + _nextActionCost;
            }

            _currentActor = null;
        }

        /// <summary>
        /// Sets the cost that will be applied to the current actor when their turn ends.
        /// The test scene uses this to make actions feel different in timeline terms.
        /// </summary>
        public void SetActionCost(int cost)
        {
            _nextActionCost = cost;
        }

        /// <summary>
        /// Gets the numeric tick currently assigned to a combatant.
        /// </summary>
        public int GetTick(ICombatant combatant)
        {
            return _ticks[combatant];
        }

        /// <summary>
        /// Picks the healthiest candidate by lowest tick, then higher speed, then initial order.
        /// </summary>
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
