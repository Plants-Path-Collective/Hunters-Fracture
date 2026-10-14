using UnityEngine;
using UnityEngine.InputSystem;
using Core;

public class GameManager : MonoBehaviour
{
    public bool isInCombat = false;
    public bool paused = false;
    public static GameManager Instance { get; private set; }
    public GameObject cmCameraActive;
    public GameObject inputsHints;
    public GameObject pauseMenu;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        var input = InputManager.Instance.Overworld;
        input.OpenPauseMenu.performed += TogglePauseMenu;
    }

    private void TogglePauseMenu(InputAction.CallbackContext context)
    {
        if (!paused)
        {
            pauseMenu.SetActive(true);
            togglePause();
        }
        else
        {
            pauseMenu.SetActive(false);
            togglePause();
        }
    }

    bool togglePause()
    {
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
            return (false);
        }
        else
        {
            Time.timeScale = 0f;
            return (true);
        }
    }
}