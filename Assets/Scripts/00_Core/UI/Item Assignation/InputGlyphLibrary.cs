using System;
using UnityEngine;

namespace Core.UI
{
    /// <summary>
    /// Maps a control path (as written in the Input Actions asset) to the sprite that shows it.
    /// Keyboard keys only need 'generic'; gamepad buttons can have one sprite per family.
    /// </summary>
    [CreateAssetMenu(menuName = "UI/Input Glyph Library")]
    public class InputGlyphLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("Control path, e.g. <Keyboard>/q or <Gamepad>/buttonSouth.")]
            public string controlPath;
            [Tooltip("Used for keyboard/mouse and as fallback for any gamepad family without its own sprite.")]
            public Sprite generic;
            public Sprite xbox;
            public Sprite playStation;
            public Sprite nintendo;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        public Sprite Find(string controlPath, InputEvent.InputType type)
        {
            foreach (Entry entry in entries)
            {
                if (!string.Equals(entry.controlPath, controlPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                Sprite specific = type switch
                {
                    InputEvent.InputType.XboxGamepad => entry.xbox,
                    InputEvent.InputType.PlayStationGamepad => entry.playStation,
                    InputEvent.InputType.NintendoGamepad => entry.nintendo,
                    _ => null
                };

                return specific != null ? specific : entry.generic;
            }

            return null;
        }
    }
}