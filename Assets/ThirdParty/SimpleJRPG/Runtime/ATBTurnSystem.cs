using System.Collections.Generic;
using System.Linq;

namespace SimpleJRPG
{
    public class ATBTurnSystem : ITurnSystem
    {
        private readonly float _gaugeMax;
        private readonly Dictionary<ICombatant, float> _gauges = new Dictionary<ICombatant, float>();
        private readonly List<ICombatant> _initOrder = new List<ICombatant>();
        private ICombatant _currentActor;

        public ATBTurnSystem(float gaugeMax = 100f)
        {
            _gaugeMax = gaugeMax;
        }

        public void Init(List<ICombatant> combatants)
        {
            _gauges.Clear();
            _initOrder.Clear();
            _currentActor = null;

            foreach (var combatant in combatants)
            {
                _gauges[combatant] = 0f;
                _initOrder.Add(combatant);
            }
        }

        public ICombatant GetNextActor()
        {
            ICombatant best = null;
            float bestGauge = 0f;

            foreach (var combatant in _initOrder)
            {
                if (!combatant.IsAlive || !_gauges.ContainsKey(combatant))
                    continue;

                float gauge = _gauges[combatant];
                if (gauge < _gaugeMax)
                    continue;

                if (best == null
                    || gauge > bestGauge
                    || (gauge == bestGauge && combatant.Speed > best.Speed))
                {
                    best = combatant;
                    bestGauge = gauge;
                }
            }

            _currentActor = best;
            return best;
        }

        public IReadOnlyList<ICombatant> GetTimeline()
        {
            return _initOrder
                .Where(c => c.IsAlive && _gauges.ContainsKey(c))
                .OrderByDescending(c => _gauges[c])
                .ToList();
        }

        public void OnActorDied(ICombatant combatant)
        {
            _gauges.Remove(combatant);
        }

        public void OnActorAdded(ICombatant combatant)
        {
            _gauges[combatant] = 0f;
            if (!_initOrder.Contains(combatant))
                _initOrder.Add(combatant);
        }

        public void OnTurnEnd()
        {
            if (_currentActor != null && _gauges.ContainsKey(_currentActor))
                _gauges[_currentActor] = 0f;

            _currentActor = null;
        }

        public void Tick(float deltaTime)
        {
            foreach (var combatant in _initOrder)
            {
                if (!combatant.IsAlive || !_gauges.ContainsKey(combatant))
                    continue;

                if (_gauges[combatant] >= _gaugeMax)
                    continue;

                _gauges[combatant] += combatant.Speed * deltaTime;
            }
        }

        public bool IsReady(ICombatant combatant)
        {
            return _gauges.ContainsKey(combatant) && _gauges[combatant] >= _gaugeMax;
        }

        public float GetGauge(ICombatant combatant)
        {
            return _gauges.ContainsKey(combatant) ? _gauges[combatant] : 0f;
        }

        public void SetGauge(ICombatant combatant, float value)
        {
            if (_gauges.ContainsKey(combatant))
                _gauges[combatant] = value;
        }

        public bool HasReadyActor()
        {
            foreach (var combatant in _initOrder)
            {
                if (combatant.IsAlive && _gauges.ContainsKey(combatant) && _gauges[combatant] >= _gaugeMax)
                    return true;
            }
            return false;
        }
    }
}
