using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CombatUnit = Core.CombatSystem.Unit.Unit;

namespace Core.CombatSystem
{
    /// <summary>
    /// Owns the combat turn order: an ordered list of Unit, index 0 = next to act. Pure
    /// positioning — no damage, no actions, no AI. CombatController consumes this (Pop/Reinsert
    /// around its own turn loop); this class never runs a turn itself.
    /// </summary>
    public class TimelineController : MonoBehaviour
    {
        private List<CombatUnit> queue = new();

        /// <summary>Read-only view for UI/CombatLog. Never mutate through this.</summary>
        public IReadOnlyList<CombatUnit> Queue => queue;

        /// <summary>
        /// Fired by every operation below. unit is null for Initialize/Clear (whole-queue
        /// events); delta carries the N for Advance/Delay, otherwise 0.
        /// </summary>
        public event Action<CombatUnit, TIMELINE_OPERATION, int> OnTimelineChanged;

        // ── Setup ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Sorts by Speed descending, then applies advantage: every unit on advantageTeam goes
        /// before every unit on the other team. Called once, when combat starts.
        /// </summary>
        public void Initialize(IEnumerable<CombatUnit> units, UNIT_TEAM advantageTeam)
        {
            List<CombatUnit> sorted = units.OrderByDescending(u => u.Speed).ToList();

            queue = sorted.Where(u => u.Team == advantageTeam)
                .Concat(sorted.Where(u => u.Team != advantageTeam))
                .ToList();

            RaiseChanged(null, TIMELINE_OPERATION.Initialize, 0);
        }

        // ── Core cycle ────────────────────────────────────────────────────────

        /// <summary>Removes and returns index 0 — the unit whose turn it is.</summary>
        public CombatUnit Pop()
        {
            if (queue.Count == 0) return null;

            CombatUnit unit = queue[0];
            queue.RemoveAt(0);
            RaiseChanged(unit, TIMELINE_OPERATION.Pop, 0);
            return unit;
        }

        /// <summary>Appends to the end. Called unconditionally when a unit's turn closes.</summary>
        public void Reinsert(CombatUnit unit)
        {
            queue.Add(unit);
            RaiseChanged(unit, TIMELINE_OPERATION.Reinsert, 0);
        }

        // ── Manipulation ──────────────────────────────────────────────────────

        public void Advance(CombatUnit unit, int n)
        {
            int index = queue.IndexOf(unit);
            if (index < 0) return;

            int target = Mathf.Max(0, index - n);
            if (target == index) return;

            Reposition(index, target, unit);
            RaiseChanged(unit, TIMELINE_OPERATION.Advance, n);
        }

        public void Delay(CombatUnit unit, int n)
        {
            int index = queue.IndexOf(unit);
            if (index < 0) return;

            int target = Mathf.Min(queue.Count - 1, index + n);
            if (target == index) return; // already at the back — no-op per spec

            Reposition(index, target, unit);
            RaiseChanged(unit, TIMELINE_OPERATION.Delay, n);
        }

        public void MoveToFront(CombatUnit unit)
        {
            int index = queue.IndexOf(unit);
            if (index <= 0) return; // not found, or already there

            Reposition(index, 0, unit);
            RaiseChanged(unit, TIMELINE_OPERATION.MoveToFront, 0);
        }

        public void MoveToBack(CombatUnit unit)
        {
            int index = queue.IndexOf(unit);
            if (index < 0 || index == queue.Count - 1) return;

            Reposition(index, queue.Count - 1, unit);
            RaiseChanged(unit, TIMELINE_OPERATION.MoveToBack, 0);
        }

        public void Swap(CombatUnit a, CombatUnit b)
        {
            if (a == null || b == null) return;
            if (!a.IsAlive || !b.IsAlive) return; // swap involving a dead unit is ignored

            int ia = queue.IndexOf(a);
            int ib = queue.IndexOf(b);
            if (ia < 0 || ib < 0) return;

            (queue[ia], queue[ib]) = (queue[ib], queue[ia]);
            RaiseChanged(a, TIMELINE_OPERATION.Swap, 0);
        }

        public void InsertAfter(CombatUnit unit, CombatUnit after)
        {
            if (unit == null || after == null || unit == after) return;

            int currentIndex = queue.IndexOf(unit);
            if (currentIndex >= 0)
                queue.RemoveAt(currentIndex);

            int afterIndex = queue.IndexOf(after);
            queue.Insert(afterIndex < 0 ? queue.Count : afterIndex + 1, unit);

            RaiseChanged(unit, TIMELINE_OPERATION.InsertAfter, 0);
        }

        /// <summary>
        /// Inserts at the front. No-op (returns false) if unit is already queued — that's what
        /// "no encadenar" means here: a unit with an extra turn already pending (or one that's
        /// simply already waiting its normal turn) can't be granted a second one right now.
        /// </summary>
        public bool GrantExtraTurn(CombatUnit unit)
        {
            if (unit == null || queue.Contains(unit)) return false;

            queue.Insert(0, unit);
            RaiseChanged(unit, TIMELINE_OPERATION.GrantExtraTurn, 0);
            return true;
        }

        // ── Death & flee ──────────────────────────────────────────────────────

        /// <summary>Removes every occurrence of unit (including a pending extra-turn duplicate).</summary>
        public void Remove(CombatUnit unit)
        {
            int removed = queue.RemoveAll(u => u == unit);
            if (removed > 0)
                RaiseChanged(unit, TIMELINE_OPERATION.Remove, removed);
        }

        /// <summary>Empties the queue entirely — successful flee.</summary>
        public void Clear()
        {
            queue.Clear();
            RaiseChanged(null, TIMELINE_OPERATION.Clear, 0);
        }

        // ── Internals ─────────────────────────────────────────────────────────

        private void Reposition(int fromIndex, int toIndex, CombatUnit unit)
        {
            queue.RemoveAt(fromIndex);
            queue.Insert(toIndex, unit);
        }

        private void RaiseChanged(CombatUnit unit, TIMELINE_OPERATION operation, int delta)
        {
            OnTimelineChanged?.Invoke(unit, operation, delta);
        }
    }
}