using System.Collections.Generic;
using UnityEngine;
using Core.CombatSystem;

namespace Core.CombatSystem.StatusSystem
{
    /// <summary>
    /// Abstract definition of a status, buff or debuff. Like ItemEffectSO, concrete subclasses
    /// define behavior through virtual hooks, so StatusBuffTracker never switches on a type.
    /// Runtime data (magnitude, remaining turns, source) lives in StatusInstance: this asset is
    /// shared by every unit that carries it.
    /// </summary>
    public abstract class StatusSO : ScriptableObject
    {
        [Header("--- Identity ---")]
        public string statusName;
        public Sprite icon;
        [Tooltip("Shown as a debuff in the UI. Stat and damage modifiers also use it as the sign of their effect.")]
        public bool isDebuff;

        /// <summary>The bearer loses its turn while it has this status.</summary>
        public virtual bool SkipsTurn => false;

        /// <summary>False when the status counts its own duration (skip-turn counts skipped turns).</summary>
        public virtual bool DecrementsOnTurnEnd => true;

        /// <summary>End of the bearer's turn, before the duration countdown. Damage, regen, etc.</summary>
        public virtual void OnTurnEnd(StatusInstance instance, CombatController combat) { }

        /// <summary>Adds this status' stat changes. Called on every recompute of the bearer's stats.</summary>
        public virtual void CollectStatModifiers(StatusInstance instance, List<TemporaryStatModifier> into) { }

        /// <summary>Multiplier applied to the damage the bearer deals.</summary>
        public virtual float OutgoingDamageMultiplier(StatusInstance instance) => 1f;
    }
}