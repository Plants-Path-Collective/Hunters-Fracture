using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.UI
{
    /// <summary>Finds the glyph sprites for an action on the device the player is using right now.</summary>
    public static class InputGlyphs
    {
        /// <summary>
        /// Fills 'result' with the sprites to show. Empty = no glyph available (use the text fallback).
        /// Composite bindings (WASD, arrows) show only their up/down parts.
        /// </summary>
        public static void Resolve(InputAction action, InputEvent.InputType type,
                                InputGlyphLibrary library, List<Sprite> result)
        {
            result.Clear();
            if (action == null || library == null) return;

            bool gamepad = type != InputEvent.InputType.KeyboardMouse;
            bool hasUp = false, hasDown = false;

            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                if (binding.isComposite) continue;

                string path = binding.effectivePath;
                if (string.IsNullOrEmpty(path) || IsKeyboardMouse(path) == gamepad) continue;

                if (binding.isPartOfComposite)
                {
                    bool isUp = string.Equals(binding.name, "up", StringComparison.OrdinalIgnoreCase);
                    bool isDown = string.Equals(binding.name, "down", StringComparison.OrdinalIgnoreCase);
                    if (!isUp && !isDown) continue;
                    if ((isUp && hasUp) || (isDown && hasDown)) continue;

                    Sprite part = library.Find(path, type);
                    if (part == null) continue; // try the next binding with the same part name

                    result.Add(part);
                    if (isUp) hasUp = true; else hasDown = true;
                    continue;
                }

                // Plain binding (e.g. <Gamepad>/dpad): only used when no composite part had a sprite.
                if (result.Count == 0)
                {
                    Sprite sprite = library.Find(path, type);
                    if (sprite != null) result.Add(sprite);
                }
                break;
            }
        }

        private static bool IsKeyboardMouse(string path) =>
            path.StartsWith("<Keyboard>", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("<Mouse>", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("<Pointer>", StringComparison.OrdinalIgnoreCase);

        private static bool IsVerticalPart(string partName) =>
            string.Equals(partName, "up", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(partName, "down", StringComparison.OrdinalIgnoreCase);
    }
}