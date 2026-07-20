using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleJRPG
{
    public class PressTurnSystem : ITurnSystem
    {
        private readonly int _maxPointsPerTeam;

        private List<ICombatant> _combatants;
        private List<int> _teams;
        private int _activeTeamIndex;
        private int _fullPoints;
        private int _halfPoints;
        private ICombatant _currentActor;
        private ICombatant _selectedActor;

        public int ActiveTeam => _teams[_activeTeamIndex];
        public int FullPoints => _fullPoints;
        public int HalfPoints => _halfPoints;
        public int TotalPoints => _fullPoints + _halfPoints;
        public bool HasActionsRemaining => TotalPoints > 0;

        public PressTurnSystem(int maxPointsPerTeam = 0)
        {
            _maxPointsPerTeam = maxPointsPerTeam;
        }

        public void Init(List<ICombatant> combatants)
        {
            _combatants = new List<ICombatant>(combatants);
            _teams = combatants.Select(c => c.Team).Distinct().OrderBy(t => t).ToList();
            _activeTeamIndex = 0;
            StartTeamPhase();
        }

        public ICombatant GetNextActor()
        {
            if (!HasActionsRemaining)
                return null;

            if (_selectedActor == null)
                return null;

            _currentActor = _selectedActor;
            _selectedActor = null;
            return _currentActor;
        }

        public IReadOnlyList<ICombatant> GetTimeline()
        {
            return _combatants.Where(c => c.IsAlive && c.Team == ActiveTeam).ToList();
        }

        public void OnActorDied(ICombatant combatant)
        {
            // KO mid-phase doesn't retroactively shrink points.
            // Dead actors just can't be selected.
        }

        public void OnActorAdded(ICombatant combatant)
        {
            _combatants.Add(combatant);

            int team = combatant.Team;
            if (!_teams.Contains(team))
            {
                _teams.Add(team);
                _teams.Sort();
            }
        }

        public void OnTurnEnd()
        {
            _currentActor = null;

            if (!HasActionsRemaining)
                AdvanceTeam();
        }

        // --- Press Turn specific: Actor Selection ---

        public void SelectActor(ICombatant combatant)
        {
            if (combatant.Team != ActiveTeam)
                throw new InvalidOperationException(
                    $"Cannot select '{combatant.Name}' (team {combatant.Team}). Active team is {ActiveTeam}.");

            if (!combatant.IsAlive)
                throw new InvalidOperationException(
                    $"Cannot select '{combatant.Name}' because they are dead.");

            _selectedActor = combatant;
        }

        // --- Press Turn specific: Point Manipulation ---

        public void ConsumeAction()
        {
            if (!HasActionsRemaining)
                throw new InvalidOperationException("No actions remaining.");

            if (_halfPoints > 0)
                _halfPoints--;
            else
                _fullPoints--;
        }

        public void ConvertAction()
        {
            if (!HasActionsRemaining)
                throw new InvalidOperationException("No actions remaining.");

            if (_fullPoints > 0)
            {
                _fullPoints--;
                _halfPoints++;
            }
            else
            {
                _halfPoints--;
            }
        }

        public void ConsumeActions(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

            for (int i = 0; i < count && HasActionsRemaining; i++)
            {
                if (_halfPoints > 0)
                    _halfPoints--;
                else
                    _fullPoints--;
            }
        }

        public void ConsumeAllActions()
        {
            _fullPoints = 0;
            _halfPoints = 0;
        }

        // --- Internals ---

        private void StartTeamPhase()
        {
            int aliveCount = _combatants.Count(c => c.IsAlive && c.Team == ActiveTeam);
            _fullPoints = _maxPointsPerTeam > 0 ? Math.Min(aliveCount, _maxPointsPerTeam) : aliveCount;
            _halfPoints = 0;
            _currentActor = null;
            _selectedActor = null;
        }

        private void AdvanceTeam()
        {
            int startIndex = _activeTeamIndex;

            for (int i = 0; i < _teams.Count; i++)
            {
                _activeTeamIndex = (_activeTeamIndex + 1) % _teams.Count;

                if (_combatants.Any(c => c.IsAlive && c.Team == _teams[_activeTeamIndex]))
                {
                    StartTeamPhase();
                    return;
                }
            }

            // No team with alive members found — battle should be over.
            // Start phase anyway with 0 points so state is consistent.
            StartTeamPhase();
        }
    }
}
