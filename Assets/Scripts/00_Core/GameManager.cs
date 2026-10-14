using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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
    public GameObject inspectionPanel;

    private InputManager inputManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Resolve the sibling singleton after every scene object's Awake has run.
        inputManager = InputManager.Instance;
        if (inputManager == null)
        {
            Debug.LogError($"[{nameof(GameManager)}] {nameof(InputManager)} is not available.", this);
            return;
        }

        inputManager.Overworld.OpenPauseMenu.performed += OnOpenPauseMenu;
        inputManager.Combat.OpenPauseMenu.performed += OnOpenPauseMenu;
        inputManager.Social.OpenPauseMenu.performed += OnOpenPauseMenu;
        inputManager.UI.CancelBack.performed += OnPauseMenuCancel;

        if (pauseMenu == null)
            Debug.LogError($"[{nameof(GameManager)}] Pause menu is not assigned. " +
                           "Assign the scene's Pause Menu object in the Inspector.", this);
    }

    private void OnOpenPauseMenu(InputAction.CallbackContext context) => TogglePauseMenu();

    private void OnPauseMenuCancel(InputAction.CallbackContext context)
    {
        if (!paused)
            return;

        if (inspectionPanel != null && inspectionPanel.activeInHierarchy)
        {
            inspectionPanel.GetComponent<InspectionPanel>().Hide();
            return;
        }

        TogglePauseMenu();
    }

    public void TogglePauseMenu()
    {
        if (pauseMenu == null)
        {
            Debug.LogError($"[{nameof(GameManager)}] Pause menu is not assigned.", this);
            return;
        }

        if (inputManager == null)
        {
            Debug.LogError($"[{nameof(GameManager)}] Cannot toggle pause because " +
                           $"{nameof(InputManager)} is not available.", this);
            return;
        }

        if (paused)
        {
            inputsHints.SetActive(true);
            pauseMenu.SetActive(false);
            Time.timeScale = 1f;
            paused = false;
            inputManager.PopUIMap();
        }
        else
        {
            inputsHints.SetActive(false);
            pauseMenu.SetActive(true);
            Time.timeScale = 0f;
            paused = true;
            inputManager.PushUIMap();
            SelectFirstPauseMenuControl();
        }
    }

    private void SelectFirstPauseMenuControl()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogError($"[{nameof(GameManager)}] Cannot select a pause-menu control: " +
                           $"{nameof(EventSystem)}.current is null.", this);
            return;
        }

        Selectable[] controls = pauseMenu.GetComponentsInChildren<Selectable>();
        foreach (Selectable control in controls)
        {
            if (control == null || !control.isActiveAndEnabled || !control.interactable)
                continue;

            eventSystem.SetSelectedGameObject(control.gameObject);
            return;
        }

        Debug.LogWarning($"[{nameof(GameManager)}] Pause menu has no active, interactable " +
                         $"{nameof(Selectable)} to focus for gamepad navigation.", this);
    }

    private void OnDestroy()
    {
        if (inputManager != null)
        {
            inputManager.Overworld.OpenPauseMenu.performed -= OnOpenPauseMenu;
            inputManager.Combat.OpenPauseMenu.performed -= OnOpenPauseMenu;
            inputManager.Social.OpenPauseMenu.performed -= OnOpenPauseMenu;
            inputManager.UI.CancelBack.performed -= OnPauseMenuCancel;
        }

        if (paused)
        {
            Time.timeScale = 1f;
            paused = false;
        }

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