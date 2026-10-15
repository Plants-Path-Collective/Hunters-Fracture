using UnityEngine;
using FMODUnity;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance {get; private set;}

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("Se encontró mas de un Audio Manager en la escena");
        }
        instance = this;
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
