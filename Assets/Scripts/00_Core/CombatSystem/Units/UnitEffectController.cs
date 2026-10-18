using System.Collections.Generic;
using UnityEngine;
using Core.CombatSystem.ItemSystem;

namespace Core.CombatSystem.Units
{
    /// <summary>
    /// Attaches/detaches a Unit's TriggeredEffectSO to whatever CombatController events they
    /// react to, ordered by Priority (higher runs first).
    /// </summary>
    /// <remarks>
    /// <para>UnitInventory.OnStackChanged carries no information about which item changed —
    /// so, same philosophy as UnitStatsController.RecalculateStats(), every firing does a full
    /// recompute: detach everything currently attached, then reattach everything that should be
    /// attached (any item with quantity &gt; 0), in Priority order. Never an incremental
    /// add/remove — that's what keeps subscription order deterministic regardless of what
    /// sequence changes happened in.</para>
    /// <para>No auto-wiring in Awake(): Unit.InitializeFromDefinition() calls Initialize(this)
    /// explicitly once everything else is set up. That avoids relying on Awake() order between
    /// sibling components, which isn't guaranteed for dynamically created Units (a test harness,
    /// say) even though it's controllable in a hand-built prefab.</para>
    /// </remarks>
    public class UnitEffectController : MonoBehaviour
    {
        private Unit unit;
        private UnitInventory inventory;

        private readonly List<TriggeredEffectSO> attachedEffects = new();

        private void Awake()
        {
            inventory = GetComponent<UnitInventory>();

            if (inventory != null)
                inventory.OnStackChanged.AddListener(Reconcile);
        }

        /// <summary>
        /// Called once by Unit.InitializeFromDefinition() after everything else is set up.
        /// Also runs an initial Reconcile() in case the starting inventory already has stock
        /// (e.g. once InitializeFromParty/inventorySnapshot exists).
        /// </summary>
        public void Initialize(Unit owner)
        {
            unit = owner;
            Reconcile();
        }

        /// <summary>
        /// Recomputes the set of TriggeredEffectSO that should be attached to this Unit, and
        /// attaches/detaches them as necessary. Called whenever the Unit's inventory changes.
        /// Also used to attach the Unit's own passives (from its UnitDefinitionSO) when entering combat.
        /// </summary>
        private void Reconcile()
        {
            if (unit == null) return;

            foreach (TriggeredEffectSO effect in attachedEffects)
                effect.Detach(unit);
            attachedEffects.Clear();

            var toAttach = new List<TriggeredEffectSO>();

            // The unit's own passives: always on, independent of its inventory.
            if (unit.Definition != null && unit.Definition.passives != null)
                foreach (TriggeredEffectSO passive in unit.Definition.passives)
                    if (passive != null) toAttach.Add(passive);

            if (inventory != null && inventory.inventory != null)
            {
                foreach (Item item in inventory.inventory.Values)
                {
                    if (item?.itemSO?.effects == null || item.quantity <= 0)
                        continue;

                    foreach (ItemEffectSO effect in item.itemSO.effects)
                        if (effect is TriggeredEffectSO triggered)
                            toAttach.Add(triggered);
                }
            }

            // Higher priority first.
            toAttach.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            foreach (TriggeredEffectSO effect in toAttach)
            {
                effect.Attach(unit);
                attachedEffects.Add(effect);
            }
        }

        /// <summary>
        /// Detaches every attached effect and forgets the owner, so later inventory changes
        /// (e.g. a pickup in the Overworld) can't call Attach() without a CombatController.
        /// EnterCombat() re-initializes it for the next encounter.
        /// </summary>
        public void DetachAll()
        {
            foreach (TriggeredEffectSO effect in attachedEffects)
                effect.Detach(unit);

            attachedEffects.Clear();
            unit = null;
        }
    }
}
