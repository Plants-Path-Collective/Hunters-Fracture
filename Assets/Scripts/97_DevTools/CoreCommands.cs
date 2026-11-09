#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using Core;

namespace DevTools
{
    /// <summary>
    /// Built-in commands that only depend on Unity and the InputManager (nothing game-specific).
    /// [Preserve] keeps the reflection-only methods from being stripped in IL2CPP development builds.
    /// </summary>
    [Preserve]
    internal static class CoreCommands
    {
        // ── Console ───────────────────────────────────────────────────────────

        [ConsoleCommand("help", "Lists all commands, or shows the usage of one.")]
        private static string Help(string command = null)
        {
            if (!string.IsNullOrWhiteSpace(command))
            {
                if (!CommandRegistry.TryGet(command.TrimStart('/'), out CommandInfo info))
                    return $"Unknown command '{command}'.";
                return info.Usage + "\n" + info.Description;
            }

            var sb = new StringBuilder("Commands ('help <command>' for usage, Tab to autocomplete, Up/Down for history):");
            foreach (CommandInfo c in CommandRegistry.All)
                sb.Append("\n  ").Append(c.Name.PadRight(20)).Append(c.Description);
            return sb.ToString();
        }

        [ConsoleCommand("clear", "Clears the console.")]
        private static void Clear() => DevConsole.Clear();

        [ConsoleCommand("echo", "Prints the given text.")]
        private static string Echo(params string[] text) => string.Join(" ", text);

        [ConsoleCommand("console.stack", "Toggles stack traces under error lines.")]
        private static string ToggleStack() =>
            "Stack traces: " + (DevConsole.ToggleStackTraces() ? "on" : "off");

        [ConsoleCommand("console.save", "Saves the console log to a text file and prints its path.")]
        private static string Save() => "Saved: " + DevConsole.SaveToFile();

        [ConsoleCommand("quit", "Quits the game (stops Play Mode in the Editor).")]
        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ── Time ──────────────────────────────────────────────────────────────

        [ConsoleCommand("timescale", "Shows or sets Time.timeScale (0 = paused, 1 = normal).")]
        private static string TimeScale(float scale = -1f)
        {
            if (scale < 0f) return $"timeScale = {Time.timeScale}";

            Time.timeScale = Mathf.Clamp(scale, 0f, 20f);
            return $"timeScale = {Time.timeScale}";
        }

        // ── Input maps ────────────────────────────────────────────────────────

        [ConsoleCommand("map", "Shows or changes the active InputAction map (applies when the console closes).")]
        private static string Map(string name = null)
        {
            if (InputManager.Instance == null) return "InputManager is not available yet.";

            if (string.IsNullOrWhiteSpace(name))
            {
                return $"Gameplay map: {DevConsole.GameplayMap}\nAvailable: " +
                       string.Join(", ", Enum.GetNames(typeof(INPUTACTION_MAP)));
            }

            if (!Enum.TryParse(name, true, out INPUTACTION_MAP map) || !Enum.IsDefined(typeof(INPUTACTION_MAP), map))
                return $"Unknown map '{name}'. Available: " + string.Join(", ", Enum.GetNames(typeof(INPUTACTION_MAP)));

            // The console keeps gameplay input blocked while open and re-applies this map on close.
            InputManager.Instance.ChangeActionMap(map);
            return $"Input map -> {map} (active once the console is closed).";
        }

        // ── Scenes ────────────────────────────────────────────────────────────

        [ConsoleCommand("scenes", "Lists the scenes in Build Settings and the currently loaded ones.")]
        private static string Scenes()
        {
            var sb = new StringBuilder("Build Settings:");
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                sb.Append($"\n  [{i}] {SceneNameAt(i)}");

            sb.Append("\nLoaded:");
            Scene active = SceneManager.GetActiveScene();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                sb.Append($"\n  {scene.name}{(scene == active ? " (active)" : string.Empty)}");
            }

            return sb.ToString();
        }

        [ConsoleCommand("scene", "Loads a build scene by index or (partial) name. Second argument: additive true/false.")]
        private static string LoadScene(string scene, bool additive = false)
        {
            if (!TryFindBuildScene(scene, out int buildIndex, out string error)) return error;

            SceneManager.LoadScene(buildIndex, additive ? LoadSceneMode.Additive : LoadSceneMode.Single);
            return $"Loading '{SceneNameAt(buildIndex)}'{(additive ? " (additive)" : string.Empty)}...";
        }

        [ConsoleCommand("scene.reload", "Reloads the active scene.")]
        private static string ReloadScene()
        {
            Scene active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.buildIndex);
            return $"Reloading '{active.name}'...";
        }

        [ConsoleCommand("scene.unload", "Unloads a currently loaded scene by name.")]
        private static string UnloadScene(string scene)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loaded = SceneManager.GetSceneAt(i);
                if (!string.Equals(loaded.name, scene, StringComparison.OrdinalIgnoreCase)) continue;

                SceneManager.UnloadSceneAsync(loaded);
                return $"Unloading '{loaded.name}'...";
            }

            return $"Scene '{scene}' is not loaded. Use 'scenes' to see the loaded ones.";
        }

        private static string SceneNameAt(int buildIndex) =>
            Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(buildIndex));

        private static bool TryFindBuildScene(string query, out int buildIndex, out string error)
        {
            buildIndex = -1;
            error = null;
            int count = SceneManager.sceneCountInBuildSettings;

            if (int.TryParse(query, out int index))
            {
                if (index >= 0 && index < count)
                {
                    buildIndex = index;
                    return true;
                }

                error = $"Build index {index} is out of range (0-{count - 1}).";
                return false;
            }

            var partial = new List<int>();
            for (int i = 0; i < count; i++)
            {
                string name = SceneNameAt(i);
                if (string.Equals(name, query, StringComparison.OrdinalIgnoreCase))
                {
                    buildIndex = i;
                    return true;
                }

                if (name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) partial.Add(i);
            }

            if (partial.Count == 1)
            {
                buildIndex = partial[0];
                return true;
            }

            error = partial.Count == 0
                ? $"No scene matches '{query}'. Use 'scenes' to list them."
                : "Ambiguous, matches: " + string.Join(", ", partial.Select(SceneNameAt));
            return false;
        }
    }
}
#endif