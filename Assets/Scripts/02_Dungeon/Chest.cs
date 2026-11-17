using UnityEngine;

namespace Dungeon
{
    public class Chest : MonoBehaviour
    {
        private bool playerCanInteract = false;

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerCanInteract = true;
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerCanInteract = false;
            }
        }
    }
}