using System;
using System.Collections.Generic;
using Core;
using Core.CombatSystem.ItemSystem;
using Core.UI;
using DG.Tweening;
using UnityEngine;

namespace Dungeon
{
    /// <summary>
    /// Allows the player to interact when near (trigger).
    /// The rewards only randomize per floor, so if Player
    /// unlocks an item, this cant be obtained in chest 
    /// until floor changes.
    /// </summary>
    public class Chest : MonoBehaviour
    {
        [SerializeField] private ItemCatalog itemCatalog;
        [Header("--- Configuration ---")]
        [SerializeField] private int rewardsQuantity = 1;
        [ReadOnly] public List<ItemSO> rewards = new List<ItemSO>(0) ;

        [Header("Placeholders, replace with 3D Models later")]
        [SerializeField] private Transform lidPivot;
        
        // ── Internal State ──────────────────────────────────────────────────
        private bool playerCanInteract = false;

        private void Start()
        {
            RandomizeRewards();
        }

        private void RandomizeRewards()
        {
            if (rewardsQuantity <= 0) rewardsQuantity = 1;
            
            List<ItemSO>canBeReward = new List<ItemSO>(0);
            foreach (ItemSO item in itemCatalog.AllItems)
            {
                if (itemCatalog.IsAvailable(item) == true)
                {
                    canBeReward.Add(item);
                }
            }

            while (rewardsQuantity > 0)
            {    
                int reward = UnityEngine.Random.Range(1, canBeReward.Count + 1);
                ItemSO choosenReward = canBeReward[reward];
                rewards.Add(choosenReward);
                rewardsQuantity--;
            }

            canBeReward.Clear();
        }

        private void Update()
        {
            if (playerCanInteract && 
                InputManager.Instance.Actions.Dungeon.Interact.WasPressedThisFrame())
                OpenChest();
        }

        private void OpenChest()
        {
            Debug.Log("[Chest] Opening chest.");
            Vector3 targetRot = new Vector3(135, 0, 0);
            lidPivot.transform.DORotate(targetRot, 0.75f);

            ItemAssignationController.TryOpen(rewards);
            
            Debug.Log("[Chest] Giving reward.");
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerCanInteract = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerCanInteract = false;
            }
        }
    }
}