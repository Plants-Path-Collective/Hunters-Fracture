using System;
using System.Collections;
using InputSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace Core.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        public static MainMenuManager Instance;

        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private Selectable mainMenuFirstSelection;
        [SerializeField] private Selectable optionsFirstSelection;
        [SerializeField] private Selectable optionsReturnSelection;

        private InputSystem_Actions inputActions;
        private EventSystem eventSystem;
        private InputActionReference navigateReference;
        private InputActionReference confirmReference;
        private InputActionReference cancelReference;

        private void Awake()
        {
            // Local singleton, scoped to the Main Menu scene only (not persisted across scenes)
            if (Instance != null && Instance != this) { Destroy(this); return; }

            Instance = this;
        }

        private void Start()
        {
            inputActions = new InputSystem_Actions();
            eventSystem = EventSystem.current;

            if (eventSystem == null)
            {
                Debug.LogError($"[{nameof(MainMenuManager)}] No {nameof(EventSystem)} is available.", this);
            }
            else
            {
                InputSystemUIInputModule uiInputModule =
                    eventSystem.GetComponent<InputSystemUIInputModule>();
                if (uiInputModule == null)
                {
                    Debug.LogError($"[{nameof(MainMenuManager)}] The EventSystem has no " +
                                   $"{nameof(InputSystemUIInputModule)}.", this);
                }
                else
                {
                    navigateReference = InputActionReference.Create(inputActions.UI.Navigate);
                    confirmReference = InputActionReference.Create(inputActions.UI.Confirm);
                    cancelReference = InputActionReference.Create(inputActions.UI.CancelBack);

                    uiInputModule.enabled = false;
                    uiInputModule.move = navigateReference;
                    uiInputModule.submit = confirmReference;
                    uiInputModule.cancel = cancelReference;
                    uiInputModule.enabled = true;
                }
            }

            inputActions.UI.Enable();
            inputActions.UI.CancelBack.performed += HandleCancelBack;
            Select(mainMenuFirstSelection);
        }

        private void OnDestroy()
        {
            if (inputActions != null)
            {
                inputActions.UI.CancelBack.performed -= HandleCancelBack;
                inputActions.Disable();
                inputActions.Dispose();
                inputActions = null;
            }

            if (navigateReference != null) Destroy(navigateReference);
            if (confirmReference != null) Destroy(confirmReference);
            if (cancelReference != null) Destroy(cancelReference);

            if (Instance == this)
                Instance = null;
        }

        private void HandleCancelBack(InputAction.CallbackContext context)
        {
            if (optionsPanel != null && optionsPanel.activeSelf)
                CloseOptions();
        }

        private void Select(Selectable selectable)
        {
            if (selectable == null || !selectable.gameObject.activeInHierarchy ||
                !selectable.IsInteractable())
                return;

            if (eventSystem == null)
            {
                Debug.LogError($"[{nameof(MainMenuManager)}] No {nameof(EventSystem)} is available.");
                return;
            }

            eventSystem.SetSelectedGameObject(selectable.gameObject);
        }

        /// <summary>
        /// Starts a new game by loading the Overworld scene (placeholder target).
        /// Hook this up to the "New Game" button.
        /// </summary>
        public void NewGame()
        {
            // SceneManager.LoadScene("Social");
            SceneManager.LoadScene("Overworld - CombatDemo1");
        }

        /// <summary>
        /// Continues an existing game by loading the Overworld scene (placeholder target).
        /// Hook this up to the "Continue" button.
        /// </summary>
        public void Continue()
        {
            throw new NotImplementedException("Save system not implemented yet. Implement a save system to load the player's progress here.");       
        }

        /// <summary>
        /// Opens the Options panel by disabling the Main Menu panel and enabling the Options panel.
        /// Hook this up to the "Options" button.
        /// </summary>
        public void OpenOptions()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(true);
            Select(optionsFirstSelection);
        }

        /// <summary>
        /// Closes the Options panel by disabling it and re-enabling the Main Menu panel.
        /// Hook this up to the "Back" button.
        /// </summary>
        public void CloseOptions()
        {
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            Select(optionsReturnSelection);
        }

        /// <summary>
        /// Quits the application. Only works in a build (has no effect in the Editor).
        /// Hook this up to the "Quit" button.
        /// </summary>
        public void QuitGame()
        {
            Application.Quit();
        }

        // ----- Localization -----
        // Language order matches the Locale list configured in the Localization Settings
        // (Project Settings > Localization > Available Locales): 0 = EN, 1 = ES, 2 = JP, 3 = ZH.

        /// <summary>
        /// Switches the active language to English.
        /// Hook this up to the "EN" language button.
        /// </summary>
        public void SetLanguageEnglish()
        {
            SetLocale(0);
        }

        /// <summary>
        /// Switches the active language to Spanish.
        /// Hook this up to the "ES" language button.
        /// </summary>
        public void SetLanguageSpanish()
        {
            SetLocale(1);
        }

        /// <summary>
        /// Switches the active language to Japanese.
        /// Hook this up to the "JP" language button.
        /// </summary>
        public void SetLanguageJapanese()
        {
            SetLocale(2);
        }

        /// <summary>
        /// Switches the active language to Chinese.
        /// Hook this up to the "ZH" language button.
        /// </summary>
        public void SetLanguageChinese()
        {
            SetLocale(3);
        }

        /// <summary>
        /// Sets the active locale by index within LocalizationSettings.AvailableLocales.Locales.
        /// Waits for the localization system to finish initializing before applying the change,
        /// so it is safe to call this from a button as soon as the Main Menu loads.
        /// </summary>
        /// <param name="localeIndex">Index of the target locale (0 = EN, 1 = ES, 2 = JP, 3 = ZH).</param>
        private void SetLocale(int localeIndex)
        {
            StartCoroutine(SetLocaleRoutine(localeIndex));
        }

        private IEnumerator SetLocaleRoutine(int localeIndex)
        {
            // Ensure the localization system (tables, available locales, etc.) is ready
            yield return LocalizationSettings.InitializationOperation;

            var locales = LocalizationSettings.AvailableLocales.Locales;

            if (localeIndex < 0 || localeIndex >= locales.Count)
            {
                Debug.LogWarning($"[MainMenuManager] Locale index {localeIndex} is out of range. Available locales: {locales.Count}.");
                yield break;
            }

            LocalizationSettings.SelectedLocale = locales[localeIndex];
        }
    }
}