using UnityEngine;
using UnityEngine.Events;
using Player;
using Core.ChallengesSystem;

namespace Core.QuestsSystem
{
    /// <summary>
    /// Collectible tied to a quest objective. The dungeon reloads on every entry, so the pickup only exists
    /// while its quest is Active and the matching objective is not complete, and it re-checks on every
    /// quest change. Collecting reports the "quest_item:{itemID}" milestone.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class QuestItemPickup : MonoBehaviour, IPlayerInteractable
    {
        [Header("Quest")]
        [Tooltip("Quest that needs this item. One of its objectives must use the milestone 'quest_item:{itemID}'.")]
        [SerializeField] private QuestSO quest;

        [Tooltip("Item identifier used in the milestone ID.")]
        [SerializeField] private string itemID;

        [Header("References")]
        [Tooltip("Child object with the model. It is toggled instead of this object so events keep arriving while hidden.")]
        [SerializeField] private GameObject visuals;

        [Header("Events")]
        [Tooltip("Raised when the player collects the item (sound, particles...).")]
        [SerializeField] private UnityEvent onCollected;

        private QuestManager manager;
        private bool playerInRange;
        private bool isAvailable;

        public Transform InteractionPoint => transform;

        private string Milestone => QuestMilestones.QuestItem(itemID);

        private void Start()
        {
            SetAvailable(false);

            if (quest == null || string.IsNullOrEmpty(itemID))
            {
                Debug.LogWarning($"[{nameof(QuestItemPickup)}] Quest or itemID not assigned.", this);
                return;
            }

            if (quest.FindObjectiveIndex(Milestone) < 0)
                Debug.LogWarning($"[{nameof(QuestItemPickup)}] '{quest.questID}' has no objective with milestone '{Milestone}'.", this);

            manager = QuestManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning($"[{nameof(QuestItemPickup)}] No QuestManager found; the pickup stays hidden.", this);
                return;
            }

            manager.OnQuestStateChanged += HandleQuestStateChanged;
            manager.OnObjectiveProgress += HandleObjectiveProgress;
            Refresh();
        }

        private void OnDestroy()
        {
            if (manager == null) return;

            manager.OnQuestStateChanged -= HandleQuestStateChanged;
            manager.OnObjectiveProgress -= HandleObjectiveProgress;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) playerInRange = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player")) playerInRange = false;
        }

        public void Interact()
        {
            if (!playerInRange || !isAvailable) return;

            // Hide first so a second interaction in the same frame cannot report twice.
            SetAvailable(false);
            onCollected?.Invoke();
            ChallengeEvents.ReportMilestone(Milestone);
        }

        private void HandleQuestStateChanged(QuestSO changed, QUEST_STATE state)
        {
            if (changed == quest) Refresh();
        }

        private void HandleObjectiveProgress(QuestSO changed, int objectiveIndex, int current, int target)
        {
            if (changed == quest) Refresh();
        }

        private void Refresh()
        {
            bool needed = false;
            if (manager.GetState(quest) == QUEST_STATE.Active)
            {
                for (int i = 0; i < quest.objectives.Count; i++)
                {
                    if (quest.objectives[i] is not MilestoneObjectiveSO objective ||
                        objective.MilestoneID != Milestone)
                        continue;

                    (int current, int target) = manager.GetObjectiveProgress(quest, i);
                    if (current < target)
                    {
                        needed = true;
                        break;
                    }
                }
            }

            SetAvailable(needed);
        }

        private void SetAvailable(bool value)
        {
            isAvailable = value;
            if (visuals != null) visuals.SetActive(value);
        }
    }
}