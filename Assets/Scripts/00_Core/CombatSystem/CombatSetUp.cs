using System;
using System.Collections.Generic;
using Core.CombatSystem.Units;

namespace Core.CombatSystem
{
    /// <summary>
    /// Short-lived bridge that carries an encounter from the Overworld to CombatStage: the
    /// player's Party, the enemy slots, who has the initial advantage, and a callback for the
    /// outcome. Whoever starts a fight calls Prepare() and then loads CombatStage;
    /// CombatController calls Consume() once and the pending slot is emptied.
    /// </summary>
    public class CombatSetUp
    {
        public AllyParty Party { get; }
        public IReadOnlyList<EnemySlotData> Enemies { get; }
        public UNIT_TEAM AdvantageTeam { get; }

        /// <summary>Invoked once with the final outcome. The Overworld side decides what to do
        /// with it (rewards, game over, destroying the EnemyEntity...).</summary>
        public Action<COMBAT_OUTCOME> OnFinished { get; }

        private static CombatSetUp pending;

        private CombatSetUp(AllyParty party, IReadOnlyList<EnemySlotData> enemies, UNIT_TEAM advantageTeam,
            Action<COMBAT_OUTCOME> onFinished)
        {
            Party = party;
            Enemies = enemies;
            AdvantageTeam = advantageTeam;
            OnFinished = onFinished;
        }

        /// <summary>Stores a new pending setup, replacing any unconsumed one.</summary>
        public static void Prepare(AllyParty allyParty, EnemyParty enemyParty, UNIT_TEAM advantageTeam,
            Action<COMBAT_OUTCOME> onFinished = null)
        {
            pending = new CombatSetUp(allyParty, enemyParty.Slots, advantageTeam, onFinished);
        }

        /// <summary>Returns the pending setup (or null) and clears the slot, so it can only be used once.</summary>
        public static CombatSetUp Consume()
        {
            CombatSetUp setUp = pending;
            pending = null;
            return setUp;
        }
    }
}