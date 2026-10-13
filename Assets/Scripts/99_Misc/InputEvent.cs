using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public class InputEvent : MonoBehaviour
{
    public enum InputType
    {
        KeyboardMouse,
        Gamepad,
        XboxGamepad,
        PlayStationGamepad,
        OtherGamepad
    }

    [Header("Optional Sprite Change")]
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite gamepadSprite;
    [SerializeField] private Sprite xboxGamepadSprite;
    [SerializeField] private Sprite playStationGamepadSprite;
    [SerializeField] private Sprite keyboardMouseSprite;

    private static int activeListenerCount;
    private static event Action<InputType> InputTypeChanged;

    public static InputType CurrentInputType { get; private set; } = InputType.KeyboardMouse;

    private void OnEnable()
    {
        InputTypeChanged += HandleInputTypeChanged;

        if (activeListenerCount++ == 0)
            UnityEngine.InputSystem.InputSystem.onEvent += DetectInputType;

        UpdateSprite(CurrentInputType);
    }

    private void OnDisable()
    {
        InputTypeChanged -= HandleInputTypeChanged;

        if (activeListenerCount > 0 && --activeListenerCount == 0)
            UnityEngine.InputSystem.InputSystem.onEvent -= DetectInputType;
    }

    private static void DetectInputType(InputEventPtr eventPtr, InputDevice device)
    {
        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
            return;

        InputType inputType;
        if (device is Gamepad gamepad)
            inputType = GetGamepadInputType(gamepad);
        else if (device is Keyboard || device is Mouse)
            inputType = InputType.KeyboardMouse;
        else
            return;

        if (inputType == CurrentInputType)
            return;

        CurrentInputType = inputType;
        InputTypeChanged?.Invoke(inputType);
    }

    private void HandleInputTypeChanged(InputType inputType)
    {
        UpdateSprite(inputType);
    }

    private void UpdateSprite(InputType inputType)
    {
        if (targetImage == null)
            return;

        Sprite sprite = inputType switch
        {
            InputType.XboxGamepad => xboxGamepadSprite != null ? xboxGamepadSprite : gamepadSprite,
            InputType.PlayStationGamepad => playStationGamepadSprite != null
                ? playStationGamepadSprite
                : gamepadSprite,
            InputType.Gamepad or InputType.OtherGamepad => gamepadSprite,
            _ => keyboardMouseSprite
        };

        targetImage.sprite = sprite;
    }

    private static bool IsGamepad(InputType inputType)
    {
        return inputType != InputType.KeyboardMouse;
    }

    private static InputType GetGamepadInputType(Gamepad gamepad)
    {
        string description = string.Join(" ",
            gamepad.description.manufacturer,
            gamepad.description.product,
            gamepad.description.interfaceName,
            gamepad.displayName,
            gamepad.GetType().Name);

        if (ContainsAny(description, "PlayStation", "DualShock", "DualSense", "Sony"))
            return InputType.PlayStationGamepad;

        if (ContainsAny(description, "Xbox", "XInput", "Microsoft"))
            return InputType.XboxGamepad;

        return InputType.OtherGamepad;
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        foreach (string term in terms)
        {
            if (value.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }
}