using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Core;

public class GameManager : MonoBehaviour
{
    public bool isInCombat = false;
    public bool paused = false;
    public static GameManager Instance { get; private set; }
    public GameObject cmCameraActive;
    public GameObject globalVisuals;
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

        private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void DestroyPersistentGameObjects()
    {
        List<GameObject> persistentRoots = new();
        foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (gameObject.scene.IsValid() &&
                gameObject.scene.name == "DontDestroyOnLoad" &&
                gameObject.transform.parent == null)
            {
                persistentRoots.Add(gameObject);
            }
        }

        foreach (GameObject root in persistentRoots)
            Destroy(root);
    }
}