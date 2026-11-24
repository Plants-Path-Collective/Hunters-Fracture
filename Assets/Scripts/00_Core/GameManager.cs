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
using Core.CombatSystem.Units;

/// <summary>
/// Persistent owner of global game state: pause, combat flag, money, scene flow and language.
/// It never touches UI objects directly; <see cref="UIManager"/> reacts to the events exposed here.
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton
    // ── Singleton ─────────────────────────────────────────────────────────────
    /// <summary>Global accessor for the single active instance of the GameManager.</summary>
    public static GameManager Instance { get; private set; }
    #endregion

    #region Events
    // ── Events ────────────────────────────────────────────────────────────────
    /// <summary>Raised after the pause state changes. The argument is the new paused state.</summary>
    public event Action<bool> OnPauseChanged;

    /// <summary>Raised after <see cref="SetInCombat"/>. The argument is the new combat state.</summary>
    public event Action<bool> OnCombatStateChanged;
    #endregion

    #region Serialized state
    // ── Serialized state ──────────────────────────────────────────────────────
    [Header("Party")]
    public AllyParty party;

    [Header("State")]
    [Tooltip("True while a combat encounter is currently active.")]
    public bool isInCombat = false;

    [Tooltip("True while the game is paused (timeScale is 0).")]
    public bool paused = false;

    [Header("Scene References")]
    [Tooltip("Cinemachine camera currently being used by the game.")]
    public GameObject cmCameraActive;

    [Tooltip("Global 3D models that do not belong to any specific zone (floor, trees, etc.).")]
    public GameObject globalVisuals;
    #endregion

    #region Internal state
    // ── Internal state ────────────────────────────────────────────────────────
    /// <summary>Currency the player currently holds.</summary>
    private float pesos;

    /// <summary>Cached <see cref="InputManager"/> resolved in <see cref="Start"/>.</summary>
    private InputManager inputManager;

    /// <summary>Pause handle pushed into the game clock while paused.</summary>
    private readonly ClockPauseHandle clockPause = new(CLOCK_PAUSE_REASON.Menu);
    #endregion

    #region Unity lifecycle
    // ── Unity lifecycle ───────────────────────────────────────────────────────
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

        SubscribeToPauseInput();
    }

    private void OnDestroy()
    {
        UnsubscribeFromPauseInput();

        if (paused)
        {
            Time.timeScale = 1f;
            paused = false;
        }

        if (Instance == this)
            Instance = null;
    }

    private void SubscribeToPauseInput()
    {
        inputManager.Dungeon.OpenPauseMenu.performed += OnOpenPauseMenu;
        inputManager.Combat.OpenPauseMenu.performed  += OnOpenPauseMenu;
        inputManager.Social.OpenPauseMenu.performed  += OnOpenPauseMenu;
    }

    private void UnsubscribeFromPauseInput()
    {
        if (inputManager == null)
            return;

        inputManager.Dungeon.OpenPauseMenu.performed -= OnOpenPauseMenu;
        inputManager.Combat.OpenPauseMenu.performed  -= OnOpenPauseMenu;
        inputManager.Social.OpenPauseMenu.performed  -= OnOpenPauseMenu;
    }
    #endregion

    #region Pause
    // ── Pause ─────────────────────────────────────────────────────────────────
    private void OnOpenPauseMenu(InputAction.CallbackContext context) => TogglePauseMenu();

    /// <summary>Opens the pause menu if the game is running, or resumes it if paused.</summary>
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
    // ── Combat state ──────────────────────────────────────────────────────────
    /// <summary>Updates the global combat flag and notifies listeners.</summary>
    public void SetInCombat(bool value)
    {
        isInCombat = value;
        OnCombatStateChanged?.Invoke(value);
    }
    #endregion

    #region Consumables
    // ── Consumables ───────────────────────────────────────────────────────────
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
    // ── Language ──────────────────────────────────────────────────────────────
    public void SetLanguageEnglish()  => SetLocale(0);
    public void SetLanguageSpanish()  => SetLocale(1);
    public void SetLanguageJapanese() => SetLocale(2);
    public void SetLanguageChinese()  => SetLocale(3);

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
    // ── Scene flow ────────────────────────────────────────────────────────────
    /// <summary>Tears down persistent objects and loads the main menu scene.</summary>
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        paused = false;
        DestroyPersistentGameObjects();
        SceneManager.LoadSceneAsync("Main Menu - CombatDemo", LoadSceneMode.Single);
    }

    /// <summary>Quits the application.</summary>
    public void QuitGame()
    {
        Application.Quit();
    }

    /// <summary>Destroys every DontDestroyOnLoad root object (used when returning to the main menu).</summary>
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

    #region Money
    // ── Money ─────────────────────────────────────────────────────────────────
    /// <summary>Amount of currency the player currently holds.</summary>
    public float Pesos => pesos;
    [ReadOnly] public float currentBalance;
    public event Action<float> PesosChanged;

    private void NotifyPesosChanged()
    {
        currentBalance = pesos;
        PesosChanged?.Invoke(pesos);
    }

    /// <summary>Tries to deduct <paramref name="amount"/> pesos. Returns false if the player cannot afford it.</summary>
    public bool TrySpendPesos(float amount)
    {
        if (amount < 0f || pesos < amount) return false;
        pesos -= amount;
        NotifyPesosChanged();
        return true;
    }

    public void AddBalance(float amount)
    {
        pesos += amount;
        NotifyPesosChanged();
    }
    #endregion

    #region PARTY
    public void SetCurrentParty(AllyParty newParty)
    {
        party = newParty;
    }
    #endregion
}