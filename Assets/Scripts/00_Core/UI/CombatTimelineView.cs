using System.Collections.Generic;
using UnityEngine;
using Core.CombatSystem;
using Core.CombatSystem.Units;

namespace Core.UI
{
    /// <summary>
    /// Vertical turn-order list. Shows the unit acting now first, then the queue in order (one
    /// full cycle). Listens to CombatController only; nothing there knows this class exists.
    /// Rebuilds once per frame at most, after every event of that frame has fired, so the
    /// transient states inside EndTurn() (reinsert, pop, next turn) are never drawn.
    /// </summary>
    public class CombatTimelineView : MonoBehaviour
    {
        [SerializeField] private CombatController combat;
        [Tooltip("Parent with a VerticalLayoutGroup. Entries are created as its children.")]
        [SerializeField] private RectTransform container;
        [SerializeField] private GameObject entryPrefab;

        private readonly List<TimelineEntryView> entries = new();
        private readonly List<Unit> order = new();
        private bool dirty;
        private bool combatActive;

        private void Awake()
        {
            if (combat == null || container == null || entryPrefab == null)
                Debug.LogError($"[{nameof(CombatTimelineView)}] Assign CombatController, container " +
                               "and entry prefab in the Inspector.");
        }

        private void OnEnable()
        {
            if (combat == null) return;

            combat.OnCombatStart += OnCombatStart;
            combat.OnTurnStart += OnUnitEvent;
            combat.OnTimelineChanged += OnTimelineChanged;
            combat.OnUnitKilled += OnUnitEvent;
            combat.OnUnitRevived += OnUnitEvent;
            combat.OnCombatEnd += OnCombatEnd;

            // In case this view was enabled after the combat already started.
            combatActive = combat.IsCombatActive;
            dirty = combatActive;
        }

        private void OnDisable()
        {
            if (combat == null) return;

            combat.OnCombatStart -= OnCombatStart;
            combat.OnTurnStart -= OnUnitEvent;
            combat.OnTimelineChanged -= OnTimelineChanged;
            combat.OnUnitKilled -= OnUnitEvent;
            combat.OnUnitRevived -= OnUnitEvent;
            combat.OnCombatEnd -= OnCombatEnd;
        }

        private void OnCombatStart(IReadOnlyList<Unit> roster)
        {
            combatActive = true;
            dirty = true;
        }

        private void OnUnitEvent(Unit unit) => dirty = true;

        private void OnTimelineChanged(Unit unit, Core.TIMELINE_OPERATION operation, int delta) => dirty = true;

        private void OnCombatEnd(Core.COMBAT_OUTCOME outcome)
        {
            combatActive = false;
            dirty = false;

            foreach (TimelineEntryView entry in entries)
                entry.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!dirty || !combatActive) return;

            dirty = false;
            Rebuild();
        }

        private void Rebuild()
        {
            order.Clear();

            // The acting unit is out of the queue while it acts, so it is listed first.
            Unit current = combat.CurrentActor;
            if (current != null && current.IsAlive)
                order.Add(current);

            foreach (Unit unit in combat.UpcomingTurns)
                order.Add(unit);

            while (entries.Count < order.Count)
            {
                GameObject instance = Instantiate(entryPrefab, container);
                TimelineEntryView entryView = instance.GetComponent<TimelineEntryView>();

                if (entryView == null)
                {
                    Debug.LogError($"[{nameof(CombatTimelineView)}] Entry prefab must contain a {nameof(TimelineEntryView)} component.");
                    Destroy(instance);
                    break;
                }

                entries.Add(entryView);
            }

            for (int i = 0; i < entries.Count; i++)
            {
                bool used = i < order.Count;
                entries[i].gameObject.SetActive(used);

                if (used)
                    entries[i].Bind(order[i], isCurrent: i == 0 && order[i] == current);
            }
        }
    }
}