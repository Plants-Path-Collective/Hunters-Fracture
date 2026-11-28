using UnityEngine.InputSystem; // add

public sealed class InputHint
{
    public string key;      // text fallback when there is no glyph
    public string label;
    public bool enabled = true;
    public int slot = -1;

    /// <summary>Action this hint represents; the view resolves its glyph for the current device.</summary>
    public InputAction action;
}