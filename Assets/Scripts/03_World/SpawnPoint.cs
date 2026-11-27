// SpawnPoint.cs
using UnityEngine;
using UnityEngine.AI;

namespace World
{
    /// <summary>
    /// Marks a place where the player can appear. If a door requested this ID,
    /// the player is moved here when the scene starts.
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private string spawnId;
        [SerializeField] private string playerTag = "Player";

        private void Start()
        {
            if (string.IsNullOrEmpty(spawnId) || SceneSpawn.PendingId != spawnId) return;
            SceneSpawn.PendingId = null;

            GameObject player = GameObject.FindWithTag(playerTag);
            if (player == null)
            {
                Debug.LogWarning($"[SpawnPoint] No object with tag '{playerTag}' found.", this);
                return;
            }

            // Moving a CharacterController or NavMeshAgent via transform gets overridden unless handled.
            CharacterController cc = player.GetComponent<CharacterController>();
            NavMeshAgent agent = player.GetComponent<NavMeshAgent>();

            if (cc != null) cc.enabled = false;
            if (agent != null) agent.Warp(transform.position);
            player.transform.SetPositionAndRotation(transform.position, transform.rotation);
            if (cc != null) cc.enabled = true;
        }
    }
}