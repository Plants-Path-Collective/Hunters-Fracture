using System;
using System.Collections.Generic;
using UnityEngine;
using Core.CombatSystem.ItemSystem;

namespace Core.UI
{
    /// <summary>
    /// Something that can receive items: a party member out of combat, or a combat Unit.
    /// The session knows nothing about where the items really live.
    /// </summary>
    public sealed class AssignTarget
    {
        public string Name;
        public Sprite Portrait;

        /// <summary>Items the target holds right now (without the pending assignments).</summary>
        public Func<List<(ItemSO item, int quantity)>> GetStacks;

        /// <summary>Writes one item into the real inventory. Called only on commit.</summary>
        public Action<ItemSO> Add;
    }

    /// <summary>
    /// Pure assignment logic: no MonoBehaviour, no UI, no input. Holds the offered items, the selection
    /// and the assignment history. Nothing is written to the targets until <see cref="TryCommit"/>.
    /// </summary>
    public sealed class ItemAssignmentSession
    {
        private struct Assignment
        {
            public ItemSO item;
            public int slot;
            public int originalIndex;
        }

        private readonly IReadOnlyList<AssignTarget> targets;
        private readonly List<ItemSO> offered = new();
        private readonly List<ItemSO> unassigned = new();
        private readonly List<Assignment> assignments = new();
        private int selectedIndex;

        /// <summary>Raised after any change the view should redraw.</summary>
        public event Action OnChanged;

        /// <summary>If true, the session can be confirmed or left with items still unassigned.</summary>
        public bool AllowLeave { get; }

        public int SlotCount => targets.Count;
        public IReadOnlyList<ItemSO> Offered => offered;
        public IReadOnlyList<ItemSO> Unassigned => unassigned;
        public int SelectedIndex => selectedIndex;
        public ItemSO Selected => unassigned.Count > 0 ? unassigned[selectedIndex] : null;
        public bool HasAssignments => assignments.Count > 0;
        public bool CanConfirm => unassigned.Count == 0 || AllowLeave;

        /// <param name="items">Items on offer. Null entries are ignored.</param>
        /// <param name="targets">One entry per slot; null = empty slot.</param>
        public ItemAssignmentSession(IEnumerable<ItemSO> items, IReadOnlyList<AssignTarget> targets, bool allowLeave)
        {
            this.targets = targets;
            AllowLeave = allowLeave;

            foreach (ItemSO item in items)
            {
                if (item == null) continue;
                offered.Add(item);
                unassigned.Add(item);
            }
        }

        public AssignTarget GetTarget(int slot) =>
            slot >= 0 && slot < targets.Count ? targets[slot] : null;

        /// <summary>Moves the selection through the unassigned items (wraps around).</summary>
        public bool MoveSelection(int direction)
        {
            if (unassigned.Count < 2) return false;

            selectedIndex = (selectedIndex + direction + unassigned.Count) % unassigned.Count;
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Assigns the selected item to a slot. False if the slot is empty or nothing is selected.</summary>
        public bool Assign(int slot)
        {
            if (GetTarget(slot) == null || unassigned.Count == 0) return false;

            ItemSO item = unassigned[selectedIndex];
            unassigned.RemoveAt(selectedIndex);
            assignments.Add(new Assignment { item = item, slot = slot, originalIndex = selectedIndex });

            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, unassigned.Count - 1));
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Undoes the last assignment (history stack). False if there is nothing to undo.</summary>
        public bool Undo()
        {
            if (assignments.Count == 0) return false;

            Assignment last = assignments[assignments.Count - 1];
            assignments.RemoveAt(assignments.Count - 1);

            int index = Mathf.Min(last.originalIndex, unassigned.Count);
            unassigned.Insert(index, last.item);
            selectedIndex = index;

            OnChanged?.Invoke();
            return true;
        }

        /// <summary>What a slot holds now plus the items assigned to it in this session.</summary>
        public List<(ItemSO item, int quantity)> GetStacks(int slot)
        {
            AssignTarget target = GetTarget(slot);
            List<(ItemSO item, int quantity)> stacks = target?.GetStacks?.Invoke() ?? new List<(ItemSO, int)>();

            foreach (Assignment assignment in assignments)
            {
                if (assignment.slot != slot) continue;

                int existing = stacks.FindIndex(s => s.item == assignment.item);
                if (existing >= 0) stacks[existing] = (assignment.item, stacks[existing].quantity + 1);
                else stacks.Add((assignment.item, 1));
            }

            return stacks;
        }

        /// <summary>
        /// Writes every assignment into its target and returns the items left unassigned.
        /// False (and nothing written) if <see cref="CanConfirm"/> is false.
        /// </summary>
        public bool TryCommit(out List<ItemSO> leftover)
        {
            leftover = null;
            if (!CanConfirm) return false;

            foreach (Assignment assignment in assignments)
                GetTarget(assignment.slot)?.Add?.Invoke(assignment.item);

            assignments.Clear();
            leftover = new List<ItemSO>(unassigned);
            return true;
        }
    }
}