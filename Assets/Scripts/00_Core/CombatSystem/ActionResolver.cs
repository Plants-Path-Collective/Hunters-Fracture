using UnityEngine;
using Core.CombatSystem.Units;

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

        /// <summary>raw = Attack − Defense/2, using the stat
        /// pair that matches the attack's damage type, reduced by DefendingReduction if the
        /// target is Defending, floored at 1.</summary>
        private int CalculateAttackDamage(Unit actor, Unit target, UNITY_TYPE damageType)
        {
            float attackStat = damageType == UNITY_TYPE.Magical ? actor.MagicPower : actor.Strength;
            float defenseStat = damageType == UNITY_TYPE.Magical ? target.MagicalDefense : target.PhysicalDefense;

            float raw = attackStat - defenseStat / 2f;

            float final = target.isDefending
                ? raw * (1f - target.Definition.defenseReduction)
                : raw;

            return Mathf.Max(1, Mathf.RoundToInt(final));
        }
    }
}