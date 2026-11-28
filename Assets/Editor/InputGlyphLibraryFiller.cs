using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Core.UI.EditorTools
{
    /// <summary>
    /// Fills the InputGlyphLibrary by matching sprite file names inside the input folders.
    /// Menu: Tools / UI / Fill Input Glyph Library. Never clears a slot; it only assigns what it finds.
    /// If a sprite is not found, edit the name tables below and run it again.
    /// </summary>
    public static class InputGlyphLibraryFiller
    {
        private const string Root = "Assets/Sprites/Inputs/";
        private const string PcFolder = Root + "PC";
        private const string XboxFolder = Root + "XBOX";
        private const string PlayStationFolder = Root + "PS5";
        private const string SwitchFolder = Root + "SWITCH";

        // Variants that lose a tie against a plain version of the same sprite.
        private static readonly string[] AvoidVariants = { "outline", "alt", "alternative" };

        private sealed class Slot
        {
            public string path;
            public string[] pc, xbox, playStation, nintendo;
        }

        // Each name is a list of alternatives, tried in order. "arrow up" matches the whole words
        // arrow + up in a file name (Arrow_Up, keyboard_arrow_up, ArrowUp...). One letter ("a") matches
        // only a standalone "a", not "Arrow".
        private static readonly string[] StickUp = { "left stick up", "stick l up", "l stick up", "ls up", "stick up" };
        private static readonly string[] StickDown = { "left stick down", "stick l down", "l stick down", "ls down", "stick down" };

        private static readonly Slot[] Slots =
        {
            // Keyboard (PC folder). "up" also appears in "UP DOWN", but the shorter name "UP" wins.
            new Slot { path = "<Keyboard>/upArrow",   pc = new[] { "up" } },
            new Slot { path = "<Keyboard>/downArrow", pc = new[] { "down" } },
            new Slot { path = "<Keyboard>/enter",     pc = new[] { "enter" } },
            new Slot { path = "<Keyboard>/backspace", pc = new[] { "backspace" } },
            new Slot { path = "<Keyboard>/a",         pc = new[] { "a" } },
            new Slot { path = "<Keyboard>/w",         pc = new[] { "w" } },
            new Slot { path = "<Keyboard>/d",         pc = new[] { "d" } },

            // Not used: no stick up/down sprites exist. Navigate uses <Gamepad>/dpad on gamepads instead.
            new Slot { path = "<Gamepad>/leftStick/up" },
            new Slot { path = "<Gamepad>/leftStick/down" },

            // Face buttons are positions: West is X on Xbox, Square on PlayStation, Y on Switch.
            new Slot { path = "<Gamepad>/buttonWest",  xbox = new[] { "x" }, playStation = new[] { "square" },   nintendo = new[] { "y" } },
            new Slot { path = "<Gamepad>/buttonNorth", xbox = new[] { "y" }, playStation = new[] { "triangle" }, nintendo = new[] { "x" } },
            new Slot { path = "<Gamepad>/buttonEast",  xbox = new[] { "b" }, playStation = new[] { "circle" },   nintendo = new[] { "a" } },

            new Slot { path = "<Gamepad>/rightTrigger", xbox = new[] { "rt" }, playStation = new[] { "r2" }, nintendo = new[] { "zr" } },
            new Slot { path = "<Gamepad>/leftTrigger",  xbox = new[] { "lt" }, playStation = new[] { "l2" }, nintendo = new[] { "zl" } },

            // Single up/down d-pad icon, used for "Select" on gamepads.
            new Slot { path = "<Gamepad>/dpad",
                xbox = new[] { "xd pad vertical" },
                playStation = new[] { "d pad vertical" },
                nintendo = new[] { "dpad vertical" } },
        };

        private sealed class Folder
        {
            public string label;
            public List<(string name, Sprite sprite)> sprites;
        }

        [MenuItem("Tools/UI/Fill Input Glyph Library")]
        private static void Fill()
        {
            InputGlyphLibrary library = Selection.activeObject as InputGlyphLibrary;
            if (library == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:InputGlyphLibrary");
                if (guids.Length == 0)
                {
                    Debug.LogError("[GlyphFiller] No InputGlyphLibrary asset found in the project.");
                    return;
                }
                library = AssetDatabase.LoadAssetAtPath<InputGlyphLibrary>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            Folder pc = Load("PC", PcFolder);
            Folder xbox = Load("XBOX", XboxFolder);
            Folder ps = Load("PS5", PlayStationFolder);
            Folder sw = Load("SWITCH", SwitchFolder);

            var so = new SerializedObject(library);
            SerializedProperty entries = so.FindProperty("entries");
            var missing = new List<string>();
            int assigned = 0;

            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                string path = entry.FindPropertyRelative("controlPath").stringValue;
                Slot slot = Slots.FirstOrDefault(s => s.path == path);
                if (slot == null)
                {
                    Debug.LogWarning($"[GlyphFiller] No name rules for '{path}', skipped.");
                    continue;
                }

                if (slot.pc == null && slot.xbox == null && slot.playStation == null && slot.nintendo == null)
                    continue; // intentionally empty

                Sprite xboxSprite = Find(xbox, slot.xbox);

                assigned += Set(entry, "generic",     slot.pc != null ? Find(pc, slot.pc) : xboxSprite, path, "generic", missing);
                assigned += Set(entry, "xbox",        xboxSprite,                                   path, "xbox", missing, slot.xbox != null);
                assigned += Set(entry, "playStation", Find(ps, slot.playStation),                   path, "playStation", missing, slot.playStation != null);
                assigned += Set(entry, "nintendo",    Find(sw, slot.nintendo),                      path, "nintendo", missing, slot.nintendo != null);
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();

            Debug.Log($"[GlyphFiller] Assigned {assigned} sprite slots in '{library.name}'.", library);

            if (missing.Count > 0)
            {
                Debug.LogWarning($"[GlyphFiller] {missing.Count} slots not found:\n" + string.Join("\n", missing));
                foreach (Folder f in new[] { pc, xbox, ps, sw })
                    Debug.Log($"[GlyphFiller] Sprites in {f.label}: " +
                              string.Join(", ", f.sprites.Select(s => s.name).OrderBy(n => n)));
            }
        }

        private static int Set(SerializedProperty entry, string field, Sprite sprite, string path,
                               string label, List<string> missing, bool expected = true)
        {
            if (!expected) return 0; // this slot does not apply to this family

            if (sprite == null)
            {
                missing.Add($"  {path} -> {label}");
                return 0;
            }

            entry.FindPropertyRelative(field).objectReferenceValue = sprite;
            return 1;
        }

        private static Folder Load(string label, string folder)
        {
            var result = new Folder { label = label, sprites = new List<(string, Sprite)>() };

            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogWarning($"[GlyphFiller] Folder not found: {folder}");
                return result;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { folder }))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                foreach (Sprite s in AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>())
                    result.sprites.Add((s.name, s));
            }

            return result;
        }

        private static Sprite Find(Folder folder, string[] alternatives)
        {
            if (alternatives == null || folder.sprites.Count == 0) return null;

            // First pass: plain names. Second pass: also split CamelCase (ArrowUp -> arrow_up).
            foreach (bool splitCamel in new[] { false, true })
            {
                foreach (string alternative in alternatives)
                {
                    string needle = "_" + alternative.Replace(' ', '_') + "_";

                    var hits = folder.sprites
                        .Where(s => Normalize(s.name, splitCamel).Contains(needle))
                        .OrderBy(s => AvoidVariants.Any(v => s.name.ToLowerInvariant().Contains(v)) ? 1 : 0)
                        .ThenBy(s => s.name.Length)
                        .ThenBy(s => s.name)
                        .ToList();

                    if (hits.Count == 0) continue;

                    if (hits.Count > 1 && hits[0].name.Length == hits[1].name.Length)
                        Debug.Log($"[GlyphFiller] '{alternative}' in {folder.label} is ambiguous " +
                                  $"({hits[0].name}, {hits[1].name}...). Picked {hits[0].name}.");

                    return hits[0].sprite;
                }
            }

            return null;
        }

        private static string Normalize(string name, bool splitCamel)
        {
            if (splitCamel) name = Regex.Replace(name, "(?<=[a-z0-9])(?=[A-Z])", "_");
            return "_" + Regex.Replace(name.ToLowerInvariant(), "[^a-z0-9]+", "_").Trim('_') + "_";
        }
    }
}