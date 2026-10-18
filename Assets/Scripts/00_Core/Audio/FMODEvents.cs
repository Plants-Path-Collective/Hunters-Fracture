using UnityEngine;
using FMODUnity;

namespace Core.Audio
{
    public class FMODEvents : MonoBehaviour
    {
        public static FMODEvents Instance  {get; private set;}

        [field: Header("Music")]
        [field: SerializeField] public EventReference CombatMusic { get; private set; }


        private void Awake()
        {
            if (Instance != null) 
            {
                Debug.LogWarning($"[{nameof(FMODEvents)}] Multiple FMODEvents instances detected. Destroying this duplicate.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
    }
}