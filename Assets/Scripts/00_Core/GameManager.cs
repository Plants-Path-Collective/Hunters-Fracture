using System.Collections;
using Core;
using Core.CombatSystem;
using Core.CombatSystem.Backpack;
using Core.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public bool isInCombat = false;
    public bool paused = false;
    public static GameManager Instance { get; private set; }
    public GameObject cmCameraActive;
    public GameObject globalVisuals;
    public GameObject inputsHints;
    public GameObject pauseMenu;

    [Header("Pause Menu Panels")]
    public GameObject mainPanel;
    public GameObject partyOverviewPanel;
    public GameObject inspectionPanel;
    public GameObject partyBackpackPanel;
    public GameObject optionsPanel;

    private InputManager inputManager;
    private InputActionReference navigateReference;
    private InputActionReference confirmReference;
    private InputActionReference cancelReference;

    [Header("--- Backpack ---")]
    private readonly List<BackpackSlotUI> backpackSlotViews = new();
    private BackpackSlotUI selectedBackpackSlot;
    [SerializeField] private GameObject backpackPanel;
    [SerializeField] private GameObject backpackSlotPrefab;
    private int backpackIndex;

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
        if (mainPanel != null) mainPanel.SetActive(false);
        if (partyOverviewPanel != null) partyOverviewPanel.SetActive(false);
        if (partyBackpackPanel != null) partyBackpackPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        // Resolve the sibling singleton after every scene object's Awake has run.
        inputManager = InputManager.Instance;
        if (inputManager == null)
        {
            Debug.LogError($"[{nameof(GameManager)}] {nameof(InputManager)} is not available.", this);
            return;
        }

        navigateReference = InputActionReference.Create(inputManager.Actions.UI.Navigate);
        confirmReference = InputActionReference.Create(inputManager.Actions.UI.Confirm);
        cancelReference = InputActionReference.Create(inputManager.Actions.UI.CancelBack);

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
        HandlePauseMenuCancel();
    }

    private void HandlePauseMenuCancel()
    {
        if (!paused)
            return;

        if (inspectionPanel != null && inspectionPanel.activeInHierarchy)
        {
            InspectionPanel inspection = inspectionPanel.GetComponent<InspectionPanel>();
            if (inspection == null)
            {
                Debug.LogError($"[{nameof(GameManager)}] Inspection panel is missing its " +
                               $"{nameof(InspectionPanel)} component.", inspectionPanel);
                return;
            }

            inspection.Hide();
            return;
        }

        if (mainPanel != null && mainPanel.activeSelf)
        {
            ClosePauseMenuPanel();
            SelectFirstPauseMenuControl();
            return;
        }

        TogglePauseMenu();
    }

    public void Resume()
    {
        HandlePauseMenuCancel();
    }

    public void OpenPartyOverview()
    {
        SetPauseMenuPanelActive(partyOverviewPanel, nameof(partyOverviewPanel));
    }

    public void OpenPartyBackpack()
    {
        RenderBackpackSlots();
        SetPauseMenuPanelActive(partyBackpackPanel, nameof(partyBackpackPanel));
    }
    public void SelectBackpackSlotIndex(int index)
    {
        if (backpackSlotViews.Count == 0)
        {
            selectedBackpackSlot = null;
            backpackIndex = 0;
            return;
        }

        index = Mathf.Clamp(index, 0, backpackSlotViews.Count - 1);
        backpackIndex = index;

        BackpackSlotUI slotUI = backpackSlotViews[index];
        SelectBackpackSlot(slotUI);
    }
    public void OpenOptions()
    {
        SetPauseMenuPanelActive(optionsPanel, nameof(optionsPanel));
    }

    public void SetLanguageEnglish()
    {
        SetLocale(0);
    }

    public void SetLanguageSpanish()
    {
        SetLocale(1);
    }

    public void SetLanguageJapanese()
    {
        SetLocale(2);
    }

    public void SetLanguageChinese()
    {
        SetLocale(3);
    }

    private void SetLocale(int localeIndex)
    {
        StartCoroutine(SetLocaleRoutine(localeIndex));
    }

    private IEnumerator SetLocaleRoutine(int localeIndex)
    {
        yield return LocalizationSettings.InitializationOperation;

        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (localeIndex < 0 || localeIndex >= locales.Count)
        {
            Debug.LogWarning($"[{nameof(GameManager)}] Locale index {localeIndex} is out of range. " +
                             $"Available locales: {locales.Count}.", this);
            yield break;
        }

        LocalizationSettings.SelectedLocale = locales[localeIndex];
    }

    private void SetPauseMenuPanelActive(GameObject panel, string panelName)
    {
        if (panel == null)
        {
            Debug.LogError($"[{nameof(GameManager)}] {panelName} is not assigned.", this);
            return;
        }

        if (mainPanel == null)
        {
            Debug.LogError($"[{nameof(GameManager)}] {nameof(mainPanel)} is not assigned.", this);
            return;
        }

        mainPanel.SetActive(true);
        if (partyOverviewPanel != panel && partyOverviewPanel != null)
            partyOverviewPanel.SetActive(false);
        if (partyBackpackPanel != panel && partyBackpackPanel != null)
            partyBackpackPanel.SetActive(false);
        if (optionsPanel != panel && optionsPanel != null)
            optionsPanel.SetActive(false);
        panel.SetActive(true);
        SelectFirstControl(panel);
    }

    private void ClosePauseMenuPanel()
    {
        if (partyOverviewPanel != null) partyOverviewPanel.SetActive(false);
        if (partyBackpackPanel != null) partyBackpackPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(false);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        paused = false;
        DestroyPersistentGameObjects();
        SceneManager.LoadSceneAsync("Main Menu - CombatDemo", LoadSceneMode.Single);
    }

    public void QuitGame()
    {
        Application.Quit();
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
            if (!isInCombat)
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
            ClosePauseMenuPanel();
            ConfigurePauseMenuInputModule();
            SelectFirstPauseMenuControl();
        }
    }

    private void ConfigurePauseMenuInputModule()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogError($"[{nameof(GameManager)}] Cannot configure pause-menu navigation: " +
                           $"{nameof(EventSystem)}.current is null.", this);
            return;
        }

        InputSystemUIInputModule uiInputModule =
            eventSystem.GetComponent<InputSystemUIInputModule>();
        if (uiInputModule == null)
        {
            Debug.LogError($"[{nameof(GameManager)}] The {nameof(EventSystem)} has no " +
                           $"{nameof(InputSystemUIInputModule)}.", this);
            return;
        }

        uiInputModule.enabled = false;
        uiInputModule.move = navigateReference;
        uiInputModule.submit = confirmReference;
        uiInputModule.cancel = cancelReference;
        uiInputModule.enabled = true;
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
            if (control == null || !control.isActiveAndEnabled || !control.IsInteractable())
                continue;

            if (mainPanel != null &&
                (control.transform == mainPanel.transform ||
                 control.transform.IsChildOf(mainPanel.transform)))
                continue;

            eventSystem.SetSelectedGameObject(control.gameObject);
            return;
        }

        Debug.LogWarning($"[{nameof(GameManager)}] Pause menu sidebar has no active, interactable " +
                         $"{nameof(Selectable)} to focus for gamepad navigation.", this);
    }

    private void RenderBackpackSlots()
    {
        if (backpackPanel == null)
        {
            Debug.LogError($"[{nameof(GameManager)}] Backpack panel is not assigned.");
            return;
        }

        VerticalLayoutGroup slotsLayout = backpackPanel.GetComponentInChildren<VerticalLayoutGroup>(true);
        if (slotsLayout == null)
        {
            Debug.LogError($"[{nameof(GameManager)}] Backpack panel has no " +
                           $"{nameof(VerticalLayoutGroup)} to contain item slots.");
            return;
        }

        if (backpackSlotPrefab == null)
        {
            Debug.LogError($"[{nameof(GameManager)}] Backpack slot prefab is not assigned.");
            return;
        }

        Transform slotsContainer = slotsLayout.transform;
        for (int i = slotsContainer.childCount - 1; i >= 0; i--)
            Destroy(slotsContainer.GetChild(i).gameObject);
        backpackSlotViews.Clear();

        if (PartyBackpack.Instance == null)
        {
            Debug.LogError($"[{nameof(GameManager)}] No {nameof(PartyBackpack)} instance is available.");
            return;
        }

        foreach (BackpackSlot slot in PartyBackpack.Instance.backpackSlots)
        {
            if (slot.consumableSO == null)
            {
                Debug.LogWarning($"[{nameof(GameManager)}] Skipping a backpack slot with no consumable.");
                continue;
            }

            GameObject slotObject = Instantiate(backpackSlotPrefab, slotsContainer, false);
            BackpackSlotUI slotUI = slotObject.GetComponent<BackpackSlotUI>();
            if (slotUI == null ||
                slotUI.consumableIcon == null ||
                slotUI.consumableNameText == null ||
                slotUI.quantityText == null ||
                slotUI.effectText == null)
            {
                Debug.LogError($"[{nameof(GameManager)}] Backpack slot prefab must have a " +
                               $"{nameof(BackpackSlotUI)} with all UI fields assigned.");
                Destroy(slotObject);
                return;
            }

            slotUI.consumableIcon.sprite = slot.consumableSO.itemIcon;
            slotUI.quantityText.text = $"x {slot.quantity}";
            slotUI.SetConsumable(slot.consumableSO);
            slotUI.SetSelected(false);

            Button button = slotObject.GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(() => SelectBackpackSlot(slotUI));
            else
                slotObject.AddComponent<Button>().onClick.AddListener(() => SelectBackpackSlot(slotUI));

            backpackSlotViews.Add(slotUI);
        }

        if (backpackSlotViews.Count > 0)
            SelectBackpackSlot(backpackSlotViews[0]);
    }

    public int GetSelectedBackpackIndex() => backpackIndex;

    public ConsumableSO GetSelectedConsumable()
    {
        return selectedBackpackSlot != null ? selectedBackpackSlot.consumable : null;
    }

    /*public bool TryConsumeSelectedBackpackItem(InputManager inputHandler)
    {
        ConsumableSO consumable = GetSelectedConsumable();
        if (consumable == null || inputHandler == null) return false;
        return inputHandler.ConsumeSelectedItem(consumable);
    }*/
    private void SelectBackpackSlot(BackpackSlotUI slotUI)
    {
        if (selectedBackpackSlot == slotUI) return;

        foreach (BackpackSlotUI other in backpackSlotViews)
            if (other != null) other.SetSelected(other == slotUI);

        selectedBackpackSlot = slotUI;
    }

    private void SelectFirstControl(GameObject selectionRoot)
    {
        if (selectionRoot == null)
            return;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogError($"[{nameof(GameManager)}] Cannot select a pause-menu control: " +
                           $"{nameof(EventSystem)}.current is null.", this);
            return;
        }

        Selectable[] controls = selectionRoot.GetComponentsInChildren<Selectable>();
        foreach (Selectable control in controls)
        {
            if (control == null || !control.isActiveAndEnabled || !control.IsInteractable())
                continue;

            eventSystem.SetSelectedGameObject(control.gameObject);
            return;
        }

        Debug.LogWarning($"[{nameof(GameManager)}] Panel '{selectionRoot.name}' has no active, " +
                         $"interactable {nameof(Selectable)} to focus for gamepad navigation.", this);
    }

    private void OnDestroy()
    {
        if (navigateReference != null) Destroy(navigateReference);
        if (confirmReference != null) Destroy(confirmReference);
        if (cancelReference != null) Destroy(cancelReference);

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