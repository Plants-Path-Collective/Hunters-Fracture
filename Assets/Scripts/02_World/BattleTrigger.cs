using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleTrigger : MonoBehaviour
{
    bool SceneLoaded = false;
    public GameObject cam;

    private void OnTriggerEnter(Collider other)
    {
        if (!SceneLoaded)
        {
            Time.timeScale = 0;
            cam.SetActive(false);
            SceneManager.LoadScene("TimelineTurnTesting", LoadSceneMode.Additive);
            SceneLoaded = true;
        }

    }
}
