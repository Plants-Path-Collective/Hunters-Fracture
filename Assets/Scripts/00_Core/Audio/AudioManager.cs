using UnityEngine;
using FMODUnity;

namespace Core.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance {get; private set;}

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[{nameof(AudioManager)}] Multiple AudioManager instances detected. Destroying this duplicate.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void PlayOneShot(EventReference sound, Vector3 worldPos)
        {
            RuntimeManager.PlayOneShot(sound, worldPos);
        }

        public void PlayOneShotNoPos(EventReference sound)
        {
            RuntimeManager.PlayOneShot(sound);
        }
    }
}