using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.UI
{
    /// <summary>One input hint shown as a pill: the key/button text plus what it does.</summary>
    public sealed class InputHint
    {
        /// <summary>Text fallback, used when there is no glyph sprite for the control.</summary>
        public string key;
        public string label;

        /// <summary>False when the action does nothing right now.</summary>
        public bool enabled = true;

        /// <summary>Party slot this hint assigns to (0..2), or -1 for a regular footer hint.</summary>
        public int slot = -1;

        /// <summary>Action this hint represents; the view resolves its glyph for the current device.</summary>
        public InputAction action;
    }

    /// <summary>
    /// Contract between the assignment controller and any visual implementation (UI Toolkit, uGUI...).
    /// A view only draws what the session says; it never reads input or changes the session.
    /// </summary>
    public abstract class ItemAssignationView : MonoBehaviour
    {
        public abstract void Show();
        public abstract void Hide();

        /// <summary>Redraws everything from the session state.</summary>
        public abstract void Render(ItemAssignmentSession session);

        /// <summary>Redraws the input hint pills.</summary>
        public abstract void SetHints(IReadOnlyList<InputHint> hints);

        /// <summary>Confirm-hold progress, 0..1. 0 hides the indicator.</summary>
        public abstract void SetHoldProgress(float normalized);
    }
}