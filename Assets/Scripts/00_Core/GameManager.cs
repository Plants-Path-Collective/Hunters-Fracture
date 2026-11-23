using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using Core;
using Core.CombatSystem.Backpack;
using Core.Clock;

/// <summary>
/// Persistent owner of global game state: pause, combat flag, money, scene flow and language.
/// It never touches UI objects directly; <see cref="UIManager"/> reacts to the events below.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>Raised after the pause state changes. The argument is the new paused state.</summary>
    public event Action<bool> OnPauseChanged;

    /// <summary>Raised after <see cref="SetInCombat"/>. The argument is the new combat state.</summary>
    public event Action<bool> OnCombatStateChanged;

    public bool isInCombat = false;
    public bool paused = false;
    public GameObject cmCameraActive;
    public GameObject globalVisuals;

    // Money
    private float pesos;
    public float Pesos => pesos;

    private InputManager inputManager;
    private readonly ClockPauseHandle clockPause = new(CLOCK_PAUSE_REASON.Menu);

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

        inputManager.Dungeon.OpenPauseMenu.performed += OnOpenPauseMenu;
        inputManager.Combat.OpenPauseMenu.performed += OnOpenPauseMenu;
        inputManager.Social.OpenPauseMenu.performed += OnOpenPauseMenu;
    }

    private void OnDestroy()
    {
        if (inputManager != null)
        {
            inputManager.Dungeon.OpenPauseMenu.performed -= OnOpenPauseMenu;
            inputManager.Combat.OpenPauseMenu.performed -= OnOpenPauseMenu;
            inputManager.Social.OpenPauseMenu.performed -= OnOpenPauseMenu;
        }

        if (paused)
        {
            Time.timeScale = 1f;
            paused = false;
        }

        if (Instance == this)
            Instance = null;
    }

    #region Pause

    private void OnOpenPauseMenu(InputAction.CallbackContext context) => TogglePauseMenu();

    public void TogglePauseMenu() => SetPaused(!paused);

    private void SetPaused(bool value)
    {
        if (paused == value)
            return;

        if (inputManager == null)
        {
            Debug.LogError($"[{nameof(GameManager)}] Cannot toggle pause because " +
                           $"{nameof(InputManager)} is not available.", this);
            return;
        }

        // Pausing without a menu to resume from would soft-lock the game.
        if (value && (UIManager.Instance == null || !UIManager.Instance.CanShowPauseMenu))
        {
            Debug.LogError($"[{nameof(GameManager)}] Cannot pause: no pause menu is available in " +
                           $"{nameof(UIManager)}.", this);
            return;
        }

        paused = value;
        Time.timeScale = value ? 0f : 1f;
        clockPause.Set(value);

        if (value)
            inputManager.PushUIMap();
        else
            inputManager.PopUIMap();

        OnPauseChanged?.Invoke(value);
    }

    #endregion

    #region Combat state

    public void SetInCombat(bool value)
    {
        isInCombat = value;
        OnCombatStateChanged?.Invoke(value);
    }

    #endregion

    #region Consumables

    /// <summary>
    /// Applies a consumable to a party member outside of combat and removes one from the backpack.
    /// Returns false (and logs why) if nothing was consumed.
    /// </summary>
    public bool TryUseConsumable(ConsumableSO consumable, Core.CombatSystem.Units.PartyMemberData member)
    {
        if (member?.definition == null || consumable == null)
        {
            Debug.LogWarning($"[{nameof(GameManager)}] Cannot use the selected consumable on an invalid party member.", this);
            return false;
        }

        PartyBackpack partyBackpack = PartyBackpack.Instance;
        if (partyBackpack == null)
        {
            Debug.LogError($"[{nameof(GameManager)}] No {nameof(PartyBackpack)} instance is available.", this);
            return false;
        }

        bool itemAvailable = false;
        foreach (BackpackSlot slot in partyBackpack.backpackSlots)
        {
            if (slot.consumableSO != consumable || slot.quantity <= 0)
                continue;

            itemAvailable = true;
            break;
        }

        if (!itemAvailable)
        {
            Debug.LogWarning($"[{nameof(GameManager)}] The selected consumable is no longer available.", this);
            return false;
        }

        int maxValue;
        switch (consumable.statType)
        {
            case STAT_TYPE.HP:
                maxValue = Mathf.Max(0, member.definition.maxHP);
                member.currentHP = RestoreStat(member.currentHP, maxValue, consumable.restoreAmount);
                break;

            case STAT_TYPE.SP:
                maxValue = Mathf.Max(0, member.definition.maxSP);
                member.currentSP = RestoreStat(member.currentSP, maxValue, consumable.restoreAmount);
                break;

            default:
                Debug.LogWarning($"[{nameof(GameManager)}] Consumable '{consumable.name}' " +
                                 $"targets {consumable.statType}, which cannot be used in the overworld.", this);
                return false;
        }

        partyBackpack.RemoveConsumable(consumable, 1);
        return true;
    }

    private static int RestoreStat(int currentValue, int maxValue, int restoreAmount)
    {
        int current = currentValue < 0 ? maxValue : Mathf.Clamp(currentValue, 0, maxValue);
        int amount = Mathf.Min(maxValue - current, Mathf.Max(0, restoreAmount));
        return current + amount;
    }

    #endregion

    #region Language

    public void SetLanguageEnglish() => SetLocale(0);

    public void SetLanguageSpanish() => SetLocale(1);

    public void SetLanguageJapanese() => SetLocale(2);

    public void SetLanguageChinese() => SetLocale(3);

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

    #endregion

    #region Scene flow

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

    #endregion

    // ── Public API ────────────────────────────────────────────────────────
    public bool TrySpendPesos(float amount)
    {
        if (pesos < amount) return false;
        pesos -= amount;
        return true;
    }
}