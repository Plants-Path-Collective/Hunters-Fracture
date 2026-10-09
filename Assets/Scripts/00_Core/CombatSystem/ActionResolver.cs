using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Core.CombatSystem.Units;
using Core.CombatSystem.SkillSystem;
namespace Core.CombatSystem
{
    /// <summary>
    /// Resolves targeting and the damage formula for a player action, then invokes the matching
    /// CombatController verb — CombatController itself never computes damage, it only applies
    /// whatever final amount it's given (see combat.html "Verbos y eventos" / "Resolución de
    /// daño"). One per encounter, sibling to CombatController on the same GameObject.
    /// </summary>
    /// <remarks>
    /// Only "Attack" is implemented so far — it's the only one of the five player actions that
    /// needs real targeting + a formula right now. Defend is simple enough to live directly
    /// on CombatController (see CombatController.Defend()); Skills and Backpack need
    /// ActionSO/the backpack system to exist first, so resolving them here would be a hollow
    /// skeleton.
    /// </remarks>
    [RequireComponent(typeof(CombatController))]
    public class ActionResolver : MonoBehaviour
    {
        private CombatController combat;
        private bool IsActorsTurn(Unit actor) =>
            combat.IsCombatActive && actor != null && actor == combat.CurrentActor;

        private void Awake()
        {
            combat = GetComponent<CombatController>();
        }

        /// <summary>
        /// Basic attack: Strength vs PhysicalDefense for a Physical attacker, MagicPower vs
        /// MagicalDefense for a Magical one (see units.html "UnitDefinitionSO" — the attacker's
        /// own UnitType decides, regardless of the target's type), reduced by the target's
        /// DefendingReduction if it's currently Defending. Always 1 enemy target — "Attack"
        /// specifically can't hit allies (see combat.html "Attack"; skills and items later will
        /// be able to target allies via their own TargetType). Returns false without doing
        /// anything if the target isn't valid.
        /// </summary>
        public bool ResolveAttack(Unit actor, Unit target)
        {
            if (!IsActorsTurn(actor))
            {
                Debug.LogWarning($"[{nameof(ActionResolver)}] {actor?.Name} tried to act out of turn.");
                return false;
            }

            if (!IsValidAttackTarget(actor, target))
            {
                Debug.LogWarning($"[{nameof(ActionResolver)}] Invalid Atacar target: " +
                    $"{actor?.Name} → {target?.Name}.");
                return false;
            }

            UNITY_TYPE damageType = actor.UnitType;
            int amount = CalculateAttackDamage(actor, target, damageType);

            combat.DealDamage(actor, target, amount, damageType, "attack");
            combat.RestoreSP(actor, actor.Definition.RollSPRegen());

            // Every player action consumes the turn (combat.html "Acciones del jugador": "todas
            // consumen el turno y reinsertan al actor al final de la cola").
            combat.EndTurn();
            return true;
        }

        private bool IsValidAttackTarget(Unit actor, Unit target)
        {
            if (actor == null || target == null) return false;
            if (!actor.IsAlive || !target.IsAlive) return false;
            return target.Team != actor.Team;
        }
        
        /// <summary>
        /// Damage Received = Base Damage / (1 + Defense), where Defense is a decimal (1.0 = 100%).
        /// Base Damage is Strength for Physical attacks and MagicPower for Magical ones. Defense
        /// already includes the Defending multiplier. Floored at 1.
        /// </summary>
        private int CalculateAttackDamage(Unit actor, Unit target, UNITY_TYPE damageType)
        {
            float baseDamage = damageType == UNITY_TYPE.Magical ? actor.MagicPower : actor.Strength;
            float defense = damageType == UNITY_TYPE.Magical
                ? target.EffectiveMagicalDefense
                : target.EffectivePhysicalDefense;

            float received = baseDamage / (1f + Mathf.Max(0f, defense));

            return Mathf.Max(1, Mathf.RoundToInt(received));
        }

        /// <summary>
        /// Resolves a technique (skill or ultimate): validates turn, SP and target, resolves who the
        /// action touches, lets OnBeforeAction interceptors adjust it, pays the SP, runs every effect in
        /// order and closes the turn. Returns false without doing anything if it cannot be used.
        /// </summary>
        public bool ResolveSkill(Unit actor, ActionSO action, Unit pickedTarget)
        {
            if (!IsActorsTurn(actor))
            {
                Debug.LogWarning($"[{nameof(ActionResolver)}] {actor?.Name} tried to act out of turn.");
                return false;
            }

            if (action == null || actor.SP < action.spCost) return false;

            if (!IsValidTarget(actor, action, pickedTarget))
            {
                Debug.LogWarning($"[{nameof(ActionResolver)}] Invalid target for {action.actionName}: " +
                                $"{actor.Name} → {pickedTarget?.Name}.");
                return false;
            }

            var context = new ActionContext { actor = actor, action = action, spCost = action.spCost };
            context.targets.AddRange(ResolveTargets(actor, action, pickedTarget));

            combat.NotifyBeforeAction(context);
            if (context.cancelled || !combat.SpendSP(actor, context.spCost)) return false;

            foreach (ActionEffectSO effect in action.effects)
                if (effect != null) effect.Apply(context, combat);

            combat.NotifyAfterAction(context);
            combat.EndTurn();
            return true;
        }

        /// <summary>Whether the picked unit is acceptable for this action. Self, None and All* actions ignore it.</summary>
        public bool IsValidTarget(Unit actor, ActionSO action, Unit target)
        {
            switch (action.targetType)
            {
                case TARGET_TYPE.SingleEnemy: return target != null && target.IsAlive && target.Team != actor.Team;
                case TARGET_TYPE.SingleAlly:  return target != null && target.IsAlive && target.Team == actor.Team;
                case TARGET_TYPE.SingleAny:   return target != null && target.IsAlive;
                default: return true;
            }
        }

        private IEnumerable<Unit> ResolveTargets(Unit actor, ActionSO action, Unit picked)
        {
            switch (action.targetType)
            {
                case TARGET_TYPE.Self: return new[] { actor };
                case TARGET_TYPE.SingleEnemy:
                case TARGET_TYPE.SingleAlly:
                case TARGET_TYPE.SingleAny: return new[] { picked };
                case TARGET_TYPE.AllEnemies: return combat.Roster.Where(u => u.IsAlive && u.Team != actor.Team);
                case TARGET_TYPE.AllAllies: return combat.Roster.Where(u => u.IsAlive && u.Team == actor.Team);
                default: return Array.Empty<Unit>();
            }
        }
    }
}