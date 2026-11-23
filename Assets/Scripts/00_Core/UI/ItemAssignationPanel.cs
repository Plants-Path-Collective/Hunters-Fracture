using System.Collections.Generic;
using Dungeon;
using UnityEngine;
using Core.CombatSystem.ItemSystem;

namespace Core.UI
{
    public class ItemAssignationPanel : MonoBehaviour
    {
        public static ItemAssignationPanel Instance;

        [ReadOnly] public List<ItemSO> itemsToAssign;
        [SerializeField] private RewardItemView itemPrefab;

        // ── Internal State ──────────────────────────────────────────────────
        private GameObject visuals;
        private RectTransform  unassignedRoot;
        private readonly List<RewardItemView> unassignedViews = new();

        private void Awake()
        {
            visuals = GetComponentInChildren<Transform>().gameObject;
        }

        public void IntializeAssignationPanel(List<ItemSO> items)
        {
            visuals.SetActive(true);

            itemsToAssign = items;

            while (unassignedViews.Count < itemsToAssign.Count)
                unassignedViews.Add(Instantiate(itemPrefab, unassignedRoot));
        }
    }
}