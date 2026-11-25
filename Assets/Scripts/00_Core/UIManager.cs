using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Core.CombatSystem.Backpack;
using Core.UI;
using Core.UI.Beeper;

namespace Core
{
    /// <summary>
    /// Persistent owner of every UI element: pause menu, party backpack screen, on-screen logs,
    /// beeper and the end-of-day screen. Game state (pause, combat flag, money) lives in
    /// <see cref="GameManager"/>; this class only reacts to it.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD")]
        [Tooltip("Input hints shown while exploring. Hidden while the game is paused.")]
        public GameObject inputsHints;

        [Header("Beeper")]
        [Tooltip("Object with the Beeper component.")]
        [SerializeField] private GameObject beeper;

        [Header("Pause Menu")]
        [Tooltip("Root object of the whole pause menu.")]
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject partyOverviewPanel;
        [SerializeField] private GameObject inspectionPanel;
        [SerializeField] private GameObject partyBackpackPanel;
        [SerializeField] private GameObject optionsPanel;

        [Header("Backpack")]
        [SerializeField] private GameObject backpackPanel;
        [SerializeField] private GameObject backpackSlotPrefab;

        /// <summary>True when a pause menu is assigned, so pausing cannot soft-lock the game.</summary>
        public bool CanShowPauseMenu => pauseMenu != null;

        private Tween endScreenTween;

        private GameManager gameManager;
        private InputManager inputManager;
        private Beeper beeperController;

        private InputActionReference navigateReference;
        private InputActionReference confirmReference;
        private InputActionReference cancelReference;

        // Logs
        private readonly Dictionary<TMP_Text, List<TimedLogMessage>> timedLogMessages = new();
        private int nextLogMessageId;
        private const float LogMessageDuration = 3f;
        public TMP_Text dungeonLogText;

        private sealed class TimedLogMessage
        {
            public int id;
            public string message;
        }

        // Backpack
        private readonly List<BackpackSlotUI> backpackSlotViews = new();
        private BackpackSlotUI selectedBackpackSlot;
        private int backpackIndex;
        private Button pauseBackpackButton;
        private bool selectingConsumableTarget;
        private ConsumableSO consumableBeingUsed;
        private PartyOverview partyOverview;

        private bool InCombat => gameManager != null && gameManager.isInCombat;

        #region Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (Instance != this) return;

            SetUpBeeper();
            SetUpPauseMenu();
            SubscribeToGameManager();
            SubscribeToInput();
            SubscribeToRunManager();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            if (navigateReference != null) Destroy(navigateReference);
            if (confirmReference != null) Destroy(confirmReference);
            if (cancelReference != null) Destroy(cancelReference);

            if (inputManager != null)
                inputManager.UI.CancelBack.performed -= OnPauseMenuCancel;

            if (gameManager != null)
            {
                gameManager.OnPauseChanged -= HandlePauseChanged;
                gameManager.OnCombatStateChanged -= HandleCombatStateChanged;
            }

            if (RunManager.Instance != null)
                RunManager.Instance.OnEndScreenRequested -= ShowEndOfDayScreen;

            Instance = null;
        }

        private void SetUpBeeper()
        {
            if (beeper == null) return;

            beeperController = beeper.GetComponent<Beeper>();
            if (beeperController == null)
                Debug.LogError($"[{nameof(UIManager)}] The assigned Beeper object is missing its " +
                               $"{nameof(Beeper)} component.", beeper);
            else
                beeperController.Hide();
        }

        private void SetUpPauseMenu()
        {
            if (pauseMenu == null)
                Debug.LogError($"[{nameof(UIManager)}] Pause menu is not assigned. " +
                               "Assign the Pause Menu object in the Inspector.", this);

            if (mainPanel != null) mainPanel.SetActive(false);
            if (partyOverviewPanel != null) partyOverviewPanel.SetActive(false);
            if (partyBackpackPanel != null) partyBackpackPanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(false);

            FindPauseBackpackButton();
            UpdatePauseBackpackButton();
        }

        private void SubscribeToGameManager()
        {
            gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                Debug.LogError($"[{nameof(UIManager)}] {nameof(GameManager)} is not available.", this);
                return;
            }

            gameManager.OnPauseChanged += HandlePauseChanged;
            gameManager.OnCombatStateChanged += HandleCombatStateChanged;
        }

        private void SubscribeToInput()
        {
            // Resolve the sibling singleton after every scene object's Awake has run.
            inputManager = InputManager.Instance;
            if (inputManager == null)
            {
                Debug.LogError($"[{nameof(UIManager)}] {nameof(InputManager)} is not available.", this);
                return;
            }

            navigateReference = InputActionReference.Create(inputManager.Actions.UI.Navigate);
            confirmReference = InputActionReference.Create(inputManager.Actions.UI.Confirm);
            cancelReference = InputActionReference.Create(inputManager.Actions.UI.CancelBack);

            inputManager.UI.CancelBack.performed += OnPauseMenuCancel;
        }

        private void SubscribeToRunManager()
        {
            if (RunManager.Instance == null)
            {
                Debug.LogError("[UIManager] Failed to subscribe to RunManager, instance is null");
                return;
            }
            RunManager.Instance.OnEndScreenRequested += ShowEndOfDayScreen;
        }

        #endregion

        #region End of day

        private void ShowEndOfDayScreen(END_CYCLE_REASONS reason)
        {
            // TODO: animate change of day, then come back to prev day and Merna vc plays repeting cycle
            InputManager.Instance.ChangeActionMap(INPUTACTION_MAP.UI);
        }

        /// <summary>Hook this to the Continue button's OnClick.</summary>
        public void OnContinuePressed() => RunManager.Instance.ConfirmEndOfDay();

        #endregion

        #region Pause menu

        private void HandlePauseChanged(bool isPaused)
        {
            if (pauseMenu == null) return;

            if (isPaused)
            {
                if (inputsHints != null) inputsHints.SetActive(false);
                pauseMenu.SetActive(true);
                ClosePauseMenuPanel();
                ConfigurePauseMenuInputModule();
                SelectFirstPauseMenuControl();
            }
            else
            {
                if (selectingConsumableTarget)
                    CancelConsumableTargetSelection();

                if (!InCombat && inputsHints != null)
                    inputsHints.SetActive(true);
                pauseMenu.SetActive(false);
            }
        }

        private void HandleCombatStateChanged(bool inCombat) => UpdatePauseBackpackButton();

        private void OnPauseMenuCancel(InputAction.CallbackContext context) => HandlePauseMenuCancel();

        private void HandlePauseMenuCancel()
        {
            if (gameManager == null || !gameManager.paused)
                return;

            if (selectingConsumableTarget)
            {
                CancelConsumableTargetSelection();
                OpenPartyBackpack();
                return;
            }

            if (inspectionPanel != null && inspectionPanel.activeInHierarchy)
            {
                InspectionPanel inspection = inspectionPanel.GetComponent<InspectionPanel>();
                if (inspection == null)
                {
                    Debug.LogError($"[{nameof(UIManager)}] Inspection panel is missing its " +
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

            gameManager.TogglePauseMenu();
        }

        /// <summary>Resume button / back action: closes the innermost open panel, then the menu.</summary>
        public void Resume() => HandlePauseMenuCancel();

        public void OpenPartyOverview()
        {
            SetPauseMenuPanelActive(partyOverviewPanel, nameof(partyOverviewPanel));
        }

        public void OpenPartyBackpack()
        {
            if (InCombat)
                return;

            RenderBackpackSlots();
            SetPauseMenuPanelActive(partyBackpackPanel, nameof(partyBackpackPanel));
            if (selectedBackpackSlot != null)
                SelectBackpackSlot(selectedBackpackSlot);
        }

        public void OpenOptions()
        {
            SetPauseMenuPanelActive(optionsPanel, nameof(optionsPanel));
        }

        private void SetPauseMenuPanelActive(GameObject panel, string panelName)
        {
            if (panel == null)
            {
                Debug.LogError($"[{nameof(UIManager)}] {panelName} is not assigned.", this);
                return;
            }

            if (mainPanel == null)
            {
                Debug.LogError($"[{nameof(UIManager)}] {nameof(mainPanel)} is not assigned.", this);
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

        private void ConfigurePauseMenuInputModule()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                Debug.LogError($"[{nameof(UIManager)}] Cannot configure pause-menu navigation: " +
                               $"{nameof(EventSystem)}.current is null.", this);
                return;
            }

            InputSystemUIInputModule uiInputModule =
                eventSystem.GetComponent<InputSystemUIInputModule>();
            if (uiInputModule == null)
            {
                Debug.LogError($"[{nameof(UIManager)}] The {nameof(EventSystem)} has no " +
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
            if (pauseMenu == null)
                return;

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                Debug.LogError($"[{nameof(UIManager)}] Cannot select a pause-menu control: " +
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

            Debug.LogWarning($"[{nameof(UIManager)}] Pause menu sidebar has no active, interactable " +
                             $"{nameof(Selectable)} to focus for gamepad navigation.", this);
        }

        private void SelectFirstControl(GameObject selectionRoot)
        {
            if (selectionRoot == null)
                return;

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                Debug.LogError($"[{nameof(UIManager)}] Cannot select a pause-menu control: " +
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

            Debug.LogWarning($"[{nameof(UIManager)}] Panel '{selectionRoot.name}' has no active, " +
                             $"interactable {nameof(Selectable)} to focus for gamepad navigation.", this);
        }

        /// <summary>
        /// Finds the pause-menu button wired (in the Inspector) to <see cref="OpenPartyBackpack"/>
        /// so it can be disabled during combat.
        /// </summary>
        private void FindPauseBackpackButton()
        {
            if (pauseMenu == null)
                return;

            foreach (Button button in pauseMenu.GetComponentsInChildren<Button>(true))
            {
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    if (button.onClick.GetPersistentTarget(i) != this ||
                        button.onClick.GetPersistentMethodName(i) != nameof(OpenPartyBackpack))
                        continue;

                    pauseBackpackButton = button;
                    return;
                }
            }

            Debug.LogWarning($"[{nameof(UIManager)}] Could not find the pause-menu button " +
                             $"wired to {nameof(OpenPartyBackpack)}.", this);
        }

        private void UpdatePauseBackpackButton()
        {
            if (pauseBackpackButton != null)
                pauseBackpackButton.interactable = !InCombat;
        }

        #endregion

        #region Beeper

        public void ToggleBeeper()
        {
            if (gameManager == null || gameManager.isInCombat || gameManager.paused ||
                inputManager == null ||
                (inputManager.CurrentMap != INPUTACTION_MAP.Dungeon &&
                 inputManager.CurrentMap != INPUTACTION_MAP.Social))
                return;

            if (beeperController == null)
            {
                Debug.LogError($"[{nameof(UIManager)}] Beeper is not assigned or does not have a " +
                               $"{nameof(Beeper)} component.", this);
                return;
            }

            beeperController.Toggle();
        }

        #endregion

        #region Logs

        public void ShowOverworldLog(string message)
        {
            if (InCombat)
                return;

            if (dungeonLogText == null)
                dungeonLogText = FindLogTextInScene(SceneManager.GetActiveScene());

            if (dungeonLogText == null)
            {
                Debug.LogError($"[{nameof(UIManager)}] Could not find a TMP text on a GameObject " +
                               "named '[Log Text]' in the active scene.");
                return;
            }

            timedLogMessages.Remove(dungeonLogText);
            dungeonLogText.text = message ?? string.Empty;
        }

        public void ShowDungeonLogTransient(string message)
        {
            if (InCombat)
                return;

            if (dungeonLogText == null)
                dungeonLogText = FindLogTextInScene(SceneManager.GetActiveScene());

            AppendTimedLog(dungeonLogText, message, "active scene");
        }

        public void ShowCombatLog(string message)
        {
            Scene combatScene = SceneManager.GetSceneByName("CombatStage");
            TMP_Text combatLogText = FindLogTextInScene(combatScene);
            AppendTimedLog(combatLogText, message, "CombatStage scene");
        }

        public void ClearDungeonLog()
        {
            if (dungeonLogText != null)
            {
                timedLogMessages.Remove(dungeonLogText);
                dungeonLogText.text = string.Empty;
            }
        }

        private void AppendTimedLog(TMP_Text text, string message, string sceneDescription)
        {
            if (text == null)
            {
                Debug.LogError($"[{nameof(UIManager)}] Could not find a TMP text on a GameObject " +
                               $"named '[Log Text]' in the {sceneDescription}.");
                return;
            }

            if (!timedLogMessages.TryGetValue(text, out List<TimedLogMessage> messages))
            {
                messages = new List<TimedLogMessage>();
                timedLogMessages.Add(text, messages);
            }

            TimedLogMessage entry = new()
            {
                id = nextLogMessageId++,
                message = message ?? string.Empty
            };
            messages.Add(entry);
            UpdateTimedLogText(text, messages);
            StartCoroutine(ExpireLogMessage(text, entry.id));
        }

        private IEnumerator ExpireLogMessage(TMP_Text text, int messageId)
        {
            yield return new WaitForSecondsRealtime(LogMessageDuration);

            if (text == null || !timedLogMessages.TryGetValue(text, out List<TimedLogMessage> messages))
                yield break;

            messages.RemoveAll(entry => entry.id == messageId);
            if (messages.Count == 0)
            {
                timedLogMessages.Remove(text);
                text.text = string.Empty;
                yield break;
            }

            UpdateTimedLogText(text, messages);
        }

        private static void UpdateTimedLogText(TMP_Text text, List<TimedLogMessage> messages)
        {
            text.text = string.Join("\n", messages.ConvertAll(entry => entry.message));
        }

        private static TMP_Text FindLogTextInScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                    if (text.gameObject.name == "[Log Text]")
                        return text;
            }

            return null;
        }

        #endregion

        #region Backpack

        public int GetSelectedBackpackIndex() => backpackIndex;

        public ConsumableSO GetSelectedConsumable()
        {
            return selectedBackpackSlot != null ? selectedBackpackSlot.consumable : null;
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

            SelectBackpackSlot(backpackSlotViews[index]);
        }

        private void RenderBackpackSlots()
        {
            if (backpackPanel == null)
            {
                Debug.LogError($"[{nameof(UIManager)}] Backpack panel is not assigned.");
                return;
            }

            GridLayoutGroup slotsLayout = backpackPanel.GetComponentInChildren<GridLayoutGroup>(true);
            if (slotsLayout == null)
            {
                Debug.LogError($"[{nameof(UIManager)}] Backpack panel has no " +
                               $"{nameof(GridLayoutGroup)} to contain item slots.");
                return;
            }

            if (backpackSlotPrefab == null)
            {
                Debug.LogError($"[{nameof(UIManager)}] Backpack slot prefab is not assigned.");
                return;
            }

            Transform slotsContainer = slotsLayout.transform;
            foreach (BackpackSlotUI slotUI in backpackSlotViews)
            {
                if (slotUI != null)
                    slotUI.Selected -= HandleBackpackSlotSelected;
            }

            for (int i = slotsContainer.childCount - 1; i >= 0; i--)
                Destroy(slotsContainer.GetChild(i).gameObject);
            backpackSlotViews.Clear();
            selectedBackpackSlot = null;
            backpackIndex = 0;

            if (PartyBackpack.Instance == null)
            {
                Debug.LogError($"[{nameof(UIManager)}] No {nameof(PartyBackpack)} instance is available.");
                return;
            }

            foreach (BackpackSlot slot in PartyBackpack.Instance.backpackSlots)
            {
                if (slot.consumableSO == null)
                {
                    Debug.LogWarning($"[{nameof(UIManager)}] Skipping a backpack slot with no consumable.");
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
                    Debug.LogError($"[{nameof(UIManager)}] Backpack slot prefab must have a " +
                                   $"{nameof(BackpackSlotUI)} with all UI fields assigned.");
                    Destroy(slotObject);
                    return;
                }

                slotUI.consumableIcon.sprite = slot.consumableSO.consumableIcon;
                slotUI.quantityText.text = $"x {slot.quantity}";
                slotUI.SetConsumable(slot.consumableSO);
                slotUI.SetSelected(false);
                slotUI.Selected += HandleBackpackSlotSelected;

                Button button = slotObject.GetComponent<Button>();
                if (button != null)
                    button.onClick.AddListener(() => HandleBackpackSlotConfirmed(slotUI));
                else
                    slotObject.AddComponent<Button>().onClick.AddListener(() => HandleBackpackSlotConfirmed(slotUI));

                backpackSlotViews.Add(slotUI);
            }

            ConfigureBackpackSlotNavigation();
            if (backpackSlotViews.Count > 0)
                SelectBackpackSlot(backpackSlotViews[0]);
        }

        private void HandleBackpackSlotSelected(BackpackSlotUI slotUI)
        {
            int index = backpackSlotViews.IndexOf(slotUI);
            if (index >= 0)
                backpackIndex = index;

            foreach (BackpackSlotUI other in backpackSlotViews)
                if (other != null && other != slotUI) other.SetSelected(false);

            selectedBackpackSlot = slotUI;
        }

        private void SelectBackpackSlot(BackpackSlotUI slotUI)
        {
            if (slotUI == null) return;
            HandleBackpackSlotSelected(slotUI);

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null && slotUI.gameObject.activeInHierarchy &&
                eventSystem.currentSelectedGameObject != slotUI.gameObject)
                eventSystem.SetSelectedGameObject(slotUI.gameObject);
        }

        private void HandleBackpackSlotConfirmed(BackpackSlotUI slotUI)
        {
            SelectBackpackSlot(slotUI);
            BeginConsumableTargetSelection(slotUI.consumable);
        }

        private void ConfigureBackpackSlotNavigation()
        {
            int count = backpackSlotViews.Count;
            for (int i = 0; i < count; i++)
            {
                Button button = backpackSlotViews[i].GetComponent<Button>();
                if (button == null) continue;

                Navigation navigation = button.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnUp = backpackSlotViews[(i + count - 1) % count].GetComponent<Button>();
                navigation.selectOnDown = backpackSlotViews[(i + 1) % count].GetComponent<Button>();
                button.navigation = navigation;
            }
        }

        #endregion

        #region Consumable target selection

        private void BeginConsumableTargetSelection(ConsumableSO consumable)
        {
            if (consumable == null)
            {
                Debug.LogWarning($"[{nameof(UIManager)}] Cannot use a null consumable.", this);
                return;
            }

            if (InCombat)
            {
                Debug.LogWarning($"[{nameof(UIManager)}] Use the combat backpack to consume items during combat.", this);
                return;
            }

            if (partyOverviewPanel == null)
            {
                Debug.LogError($"[{nameof(UIManager)}] Party overview panel is not assigned.", this);
                return;
            }

            partyOverview = partyOverviewPanel.GetComponentInChildren<PartyOverview>(true);
            if (partyOverview == null)
            {
                Debug.LogError($"[{nameof(UIManager)}] Party overview panel has no " +
                               $"{nameof(PartyOverview)} component.", partyOverviewPanel);
                return;
            }

            consumableBeingUsed = consumable;
            selectingConsumableTarget = true;
            ShowOverworldLog("Select a unit to use this consumable on.");
            SetPauseMenuPanelActive(partyOverviewPanel, nameof(partyOverviewPanel));
            partyOverview.BeginConsumableTargetSelection(UseConsumableOnPartyMember);
            SelectFirstControl(partyOverviewPanel);
        }

        private void UseConsumableOnPartyMember(Core.CombatSystem.Units.PartyMemberData member)
        {
            ClearDungeonLog();

            bool used = gameManager != null && gameManager.TryUseConsumable(consumableBeingUsed, member);
            if (used && partyOverview != null)
                partyOverview.Refresh();

            CancelConsumableTargetSelection();
            OpenPartyBackpack();
        }

        private void CancelConsumableTargetSelection()
        {
            ClearDungeonLog();
            if (partyOverview != null)
                partyOverview.CancelConsumableTargetSelection();

            selectingConsumableTarget = false;
            consumableBeingUsed = null;
            partyOverview = null;
        }

        #endregion
    }
}