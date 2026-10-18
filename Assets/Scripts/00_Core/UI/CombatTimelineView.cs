using System.Collections.Generic;
using UnityEngine;
using Core.CombatSystem;
using Core.CombatSystem.Units;
using DG.Tweening;

namespace Core.UI
{
    /// <summary>
    /// Vertical turn-order list. Shows the unit acting now first, then the queue in order (one
    /// full cycle). Listens to CombatController only; nothing there knows this class exists.
    /// Rebuilds once per frame at most, after every event of that frame has fired, so the
    /// transient states inside EndTurn() (reinsert, pop, next turn) are never drawn.
    /// </summary>
    /// <remarks>
    /// One rule drives every reorder: each row tweens to the slot of its index. Rows stay attached
    /// to their Unit, so Advance/Delay/Swap/InsertAfter look like rows sliding. Two cases fade
    /// instead of sliding: a unit that just finished its turn (fades at the top, reappears at the
    /// back) and units entering or leaving the list (inserted turn, revive, death).
    /// </remarks>
    public class CombatTimelineView : MonoBehaviour
    {
        [SerializeField] private CombatController combat;
        [Tooltip("Origin of the list: row 0 sits at its top edge. It must NOT have a LayoutGroup " +
                 "or ContentSizeFitter (they would fight the tweens).")]
        [SerializeField] private RectTransform container;
        [Tooltip("Root must use point anchors (top-center) and a fixed size.")]
        [SerializeField] private TimelineEntryView entryPrefab;
        [SerializeField, Min(0f)] private float spacing = 8f;

        [Header("--- Animation ---")]
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.15f;
        [SerializeField, Min(0f)] private float slideDuration = 0.25f;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.15f;

        private readonly List<TimelineEntryView> entries = new();
        private readonly List<Unit> order = new();
        private Unit previousTop;
        private float step;
        
        /// <summary>Vertical distance between consecutive slots, measured on a real row.</summary>
        private float Step
        {
            get
            {
                if (step <= 0f && entries.Count > 0)
                {
                    float height = ((RectTransform)entries[0].transform).rect.height;
                    if (height < 1f)
                        Debug.LogWarning($"[{nameof(CombatTimelineView)}] The entry prefab has no height: " +
                                        "give its root a fixed size and point anchors (top-center).");

                    step = height + spacing;
                }

                return step;
            }
        }

        private bool dirty;
        private bool combatActive;



        private void Awake()
        {
            if (combat == null || container == null || entryPrefab == null)
            {
                Debug.LogError($"[{nameof(CombatTimelineView)}] Assign CombatController, container " +
                               "and entry prefab in the Inspector.");
                return;
            }
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
            foreach (TimelineEntryView entry in entries)
                entry.KillTweens();

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

        private void OnTimelineChanged(Unit unit, TIMELINE_OPERATION operation, int delta) => dirty = true;

        private void OnCombatEnd(COMBAT_OUTCOME outcome)
        {
            combatActive = false;
            dirty = false;
            previousTop = null;

            foreach (TimelineEntryView entry in entries)
                entry.Release();
        }

        private void LateUpdate()
        {
            if (!dirty || !combatActive) return;

            dirty = false;
            Rebuild();
        }

        private void BuildOrder()
        {
            order.Clear();

            // The acting unit is out of the queue while it acts, so it is listed first.
            Unit current = combat.CurrentActor;
            if (current != null && current.IsAlive)
                order.Add(current);

            foreach (Unit unit in combat.UpcomingTurns)
                order.Add(unit);
        }

        private void Rebuild()
        {
            BuildOrder();

            // 1) Match each listed unit with the row that already shows it. Rows left over belong
            //    to units that are no longer listed (dead).
            List<TimelineEntryView> leftovers = entries.FindAll(e => e.InUse);
            bool firstBuild = leftovers.Count == 0;

            var rows = new TimelineEntryView[order.Count];
            var isNew = new bool[order.Count];

            for (int i = 0; i < order.Count; i++)
            {
                int found = leftovers.FindIndex(e => e.Unit == order[i]);
                if (found < 0) continue;

                rows[i] = leftovers[found];
                leftovers.RemoveAt(found);
            }

            for (int i = 0; i < order.Count; i++)
            {
                if (rows[i] != null) continue;

                rows[i] = AcquireEntry();
                isNew[i] = true;
            }

            // 2) The unit that was acting and is no longer on top just finished its turn.
            int wrappedIndex = previousTop != null ? order.IndexOf(previousTop) : -1;
            bool wrapped = wrappedIndex > 0 && !isNew[wrappedIndex];

            // Slides wait for a fade-out in progress, so the order is: fade out → slide.
            float slideDelay = wrapped || leftovers.Count > 0 ? fadeOutDuration : 0f;

            // 3) Every row goes to the slot of its index.
            Unit current = combat.CurrentActor;

            for (int i = 0; i < rows.Length; i++)
            {
                TimelineEntryView row = rows[i];
                float target = -i * Step;

                row.KillTweens();
                row.Bind(order[i], isCurrent: i == 0 && order[i] == current);

                if (isNew[i])
                {
                    // Entering the list (combat start, inserted turn, revive).
                    row.SetY(target);
                    row.SetAlpha(firstBuild ? 1f : 0f);
                    if (!firstBuild)
                        row.FadeTo(1f, fadeInDuration, slideDelay);
                }
                else if (wrapped && i == wrappedIndex)
                {
                    // Finished its turn: fades out where it is, then reappears at the back.
                    row.FadeTo(0f, fadeOutDuration).OnComplete(() =>
                    {
                        row.SetY(target);
                        row.FadeTo(1f, fadeInDuration);
                    });
                }
                else if (row.Alpha < 0.99f)
                {
                    // A previous fade was cut short: don't slide a half-transparent row.
                    row.SetY(target);
                    row.FadeTo(1f, fadeInDuration);
                }
                else if (Mathf.Abs(row.Y - target) > 0.5f)
                {
                    row.MoveTo(target, slideDuration, slideDelay);
                }
            }

            // 4) Units that left the list fade where they stand, then return to the pool.
            foreach (TimelineEntryView gone in leftovers)
            {
                gone.KillTweens();
                gone.FadeTo(0f, fadeOutDuration).OnComplete(gone.Release);
            }

            previousTop = order.Count > 0 ? order[0] : null;
        }

        private TimelineEntryView AcquireEntry()
        {
            TimelineEntryView entry = entries.Find(e => !e.InUse);

            if (entry == null)
            {
                entry = Instantiate(entryPrefab, container);
                entries.Add(entry);
            }

            // Taken right away: Bind() runs later, and the next call must not get the same row.
            entry.Claim();
            return entry;
        }
    }
}