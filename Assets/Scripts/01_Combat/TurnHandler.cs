using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using CombatSystem.UnitSystem;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CombatSystem
{
    /// <summary>
    /// Handles a single unit's turn:
    ///   - Ally unit  → switches to Combat input map, waits for player selection
    ///   - Enemy unit → queries BehaviorSO, picks action automatically
    ///
    /// Fires OnTurnResolved when the action has been passed to ActionResolver.
    /// CombatManager listens to this event to resume ATB and check end conditions.
    /// </summary>
    public class TurnHandler : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────
        public event Action OnTurnResolved;
        public event Action OnFled;

        // ── Dependencies ──────────────────────────────────────────────────────
        [SerializeField] private ActionResolver      actionResolver;
        [SerializeField] private UltimateInputHandler ultimateInput;

        // ── State ─────────────────────────────────────────────────────────────
        private Unit       _activeUnit;
        private List<Unit> _allies;
        private List<Unit> _enemies;
        private bool       _turnResolved;

        // ── Public API ────────────────────────────────────────────────────────

        public void SetParties(List<Unit> allies, List<Unit> enemies)
        {
            _allies  = allies;
            _enemies = enemies;
        }

        public void HandleTurn(Unit unit)
        {
            _activeUnit = unit;

            if (unit.UnitType == UNIT_TYPE.AllyUnit)
                StartCoroutine(HandleAllyTurn(unit));
            else
                StartCoroutine(HandleEnemyTurn(unit));
        }

        // ── Ally Turn ─────────────────────────────────────────────────────────

        private IEnumerator HandleAllyTurn(Unit unit)
        {
            InputManager.Instance.ChangeActionMap(INPUTACTION_MAP.Combat);

            // Start listening for L+R shoulder Ultimate charge
            if (ultimateInput != null)
            {
                ultimateInput.OnUltimateCharged += OnUltimateCharged;
                ultimateInput.StartListening();
            }

            _turnResolved = false;
            yield return new WaitUntil(() => _turnResolved);

            if (ultimateInput != null)
            {
                ultimateInput.StopListening();
                ultimateInput.OnUltimateCharged -= OnUltimateCharged;
            }
        }

        // Called by UltimateInputHandler when L+R are held for the full duration
        private void OnUltimateCharged()
        {
            // Build targets for the ultimate (default: all enemies)
            var targets = AlivesIn(_enemies);
            ExecuteUltimate(targets);
        }

        // ── Execute methods — called by CombatUI (ally) ───────────────────────

        public void ExecuteBasicAttack(Unit target)
        {
            actionResolver.ResolveBasicAttack(_activeUnit, target, _activeUnit.Sheet.damageType);
            FinishTurn();
        }

        public void ExecuteSkill(SkillSO skill, int skillIndex, List<Unit> targets)
        {
            if (!_activeUnit.CanUseSkill(skillIndex)) return;
            actionResolver.ResolveSkill(skill, _activeUnit, targets, skillIndex);
            FinishTurn();
        }

        public void ExecuteUltimate(List<Unit> targets)
        {
            SkillSO[] skills = _activeUnit.Sheet.skills;
            if (skills == null) return;

            for (int i = 0; i < skills.Length; i++)
            {
                if (!skills[i].isUltimate) continue;
                if (!_activeUnit.CanUseSkill(i))
                {
                    Debug.Log($"[TurnHandler] {_activeUnit.UnitName}'s ultimate has no uses left.");
                    return;
                }
                actionResolver.ResolveSkill(skills[i], _activeUnit, targets, i);
                FinishTurn();
                return;
            }

            Debug.Log($"[TurnHandler] {_activeUnit.UnitName} has no ultimate skill defined.");
        }

        public void ExecuteFlee()
        {
            if (actionResolver.TryFlee())
            {
                OnFled?.Invoke();
                return;
            }

            Debug.Log("[TurnHandler] Flee failed — turn consumed.");
            FinishTurn();
        }

        // ── Enemy Turn ────────────────────────────────────────────────────────

        private IEnumerator HandleEnemyTurn(Unit unit)
        {
            yield return new WaitForSeconds(0.5f);

            BehaviorSO behavior = unit.Sheet.behavior;
            if (behavior == null)
            {
                Unit fallback = GetPriorityTarget(TARGET_PRIORITY.Random, _allies);
                if (fallback != null)
                    actionResolver.ResolveBasicAttack(unit, fallback, unit.Sheet.damageType);
                FinishTurn();
                yield break;
            }

            string action = RollWeightedAction(unit, behavior);

            switch (action)
            {
                case "skill":
                {
                    SkillSO skill    = PickEnemySkill(unit, isUltimate: false);
                    int     skillIdx = skill != null ? Array.IndexOf(unit.Sheet.skills, skill) : -1;

                    if (skill != null && skillIdx >= 0 && unit.CanUseSkill(skillIdx))
                    {
                        actionResolver.ResolveSkill(skill, unit, ResolveTargets(skill.targetType, unit), skillIdx);
                        break;
                    }
                    goto case "attack"; // fallback if no skill available
                }

                case "ultimate":
                {
                    SkillSO ultimate = PickEnemySkill(unit, isUltimate: true);
                    int     skillIdx = ultimate != null ? Array.IndexOf(unit.Sheet.skills, ultimate) : -1;

                    if (ultimate != null && skillIdx >= 0 && unit.CanUseSkill(skillIdx))
                    {
                        actionResolver.ResolveSkill(ultimate, unit, ResolveTargets(ultimate.targetType, unit), skillIdx);
                        break;
                    }
                    goto case "attack";
                }

                case "attack":
                default:
                {
                    Unit target = GetPriorityTarget(behavior.targetPriority, _allies);
                    if (target != null)
                        actionResolver.ResolveBasicAttack(unit, target, unit.Sheet.damageType);
                    break;
                }
            }

            FinishTurn();
        }

        // ── Shared helpers ────────────────────────────────────────────────────

        private void FinishTurn()
        {
            _activeUnit.ResetATB();
            _turnResolved = true;
            InputManager.Instance.ChangeActionMap(INPUTACTION_MAP.Empty);
            OnTurnResolved?.Invoke();
        }

        private static string RollWeightedAction(Unit unit, BehaviorSO behavior)
        {
            float hpPercent   = unit.CurrentHP / unit.MaxHP;
            float skillWeight = behavior.skillWeight;
            if (hpPercent < behavior.useSkillBelowHPPercent)
                skillWeight *= 2f;

            float total = behavior.attackWeight + skillWeight + behavior.ultimateWeight;
            float roll  = Random.Range(0f, total);

            if (roll < behavior.attackWeight)                return "attack";
            if (roll < behavior.attackWeight + skillWeight)  return "skill";
            return "ultimate";
        }

        private static SkillSO PickEnemySkill(Unit unit, bool isUltimate)
        {
            if (unit.Sheet.skills == null) return null;
            foreach (SkillSO skill in unit.Sheet.skills)
                if (skill.isUltimate == isUltimate) return skill;
            return null;
        }

        private List<Unit> ResolveTargets(SKILL_TARGET targetType, Unit caster)
        {
            bool       casterIsEnemy = caster.UnitType == UNIT_TYPE.EnemyUnit;
            List<Unit> friendlies    = casterIsEnemy ? _enemies : _allies;
            List<Unit> foes          = casterIsEnemy ? _allies  : _enemies;
            var        result        = new List<Unit>();

            switch (targetType)
            {
                case SKILL_TARGET.SingleEnemy:
                    var t = GetPriorityTarget(TARGET_PRIORITY.Random, foes);
                    if (t != null) result.Add(t);
                    break;

                case SKILL_TARGET.AllEnemies:
                    result.AddRange(AlivesIn(foes));
                    break;

                case SKILL_TARGET.SingleAlly:
                    var a = GetPriorityTarget(TARGET_PRIORITY.LowestHP, friendlies);
                    if (a != null) result.Add(a);
                    break;

                case SKILL_TARGET.AllAllies:
                    result.AddRange(AlivesIn(friendlies));
                    break;

                case SKILL_TARGET.Self:
                    result.Add(caster);
                    break;

                case SKILL_TARGET.RandomEnemy:
                    var r = GetPriorityTarget(TARGET_PRIORITY.Random, foes);
                    if (r != null) result.Add(r);
                    break;

                case SKILL_TARGET.AllUnits:
                    result.AddRange(AlivesIn(_allies));
                    result.AddRange(AlivesIn(_enemies));
                    break;
            }

            return result;
        }

        // Replaces MinBy/MaxBy — not available in .NET Standard 2.1 (Unity's runtime)
        private static Unit GetPriorityTarget(TARGET_PRIORITY priority, List<Unit> pool)
        {
            List<Unit> alive = AlivesIn(pool);
            if (alive.Count == 0) return null;

            switch (priority)
            {
                case TARGET_PRIORITY.LowestHP:
                {
                    Unit best = alive[0];
                    for (int i = 1; i < alive.Count; i++)
                        if (alive[i].CurrentHP < best.CurrentHP) best = alive[i];
                    return best;
                }
                case TARGET_PRIORITY.HighestHP:
                {
                    Unit best = alive[0];
                    for (int i = 1; i < alive.Count; i++)
                        if (alive[i].CurrentHP > best.CurrentHP) best = alive[i];
                    return best;
                }
                case TARGET_PRIORITY.LowestSpeed:
                {
                    Unit best = alive[0];
                    for (int i = 1; i < alive.Count; i++)
                        if (alive[i].Speed < best.Speed) best = alive[i];
                    return best;
                }
                case TARGET_PRIORITY.HighestSpeed:
                {
                    Unit best = alive[0];
                    for (int i = 1; i < alive.Count; i++)
                        if (alive[i].Speed > best.Speed) best = alive[i];
                    return best;
                }
                default:
                    return alive[Random.Range(0, alive.Count)];
            }
        }

        private static List<Unit> AlivesIn(List<Unit> pool)
        {
            var result = new List<Unit>();
            foreach (Unit u in pool)
                if (u.State == UNIT_STATE.Alive) result.Add(u);
            return result;
        }
    }
}