#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace DevTools
{
    /// <summary>Visual category of a console line (decides its color).</summary>
    public enum CONSOLE_LINE_KIND { Log, Warning, Error, Command, Output }

    /// <summary>
    /// In-game developer console built with UI Toolkit. Press F4 to open: timestamped log on the left,
    /// command bar at the bottom.
    ///
    /// - Self-bootstraps before the first scene loads (no prefab, no UXML/USS assets, works from any scene).
    /// - Compiled only in the Editor and Development Builds.
    /// - While open it forces the InputManager to the Empty map so typing never triggers gameplay,
    ///   and restores the previous map when closed.
    /// - Log text is selectable (click + drag, Ctrl+C) and wraps natively.
    ///
    /// Optional: create a PanelSettings asset (Create > UI Toolkit > Panel Settings) named
    /// "DevConsolePanelSettings" inside any Resources folder to override the runtime-created one.
    /// </summary>
    public class DevConsole : MonoBehaviour
    {
        // ── Settings ──────────────────────────────────────────────────────────
        private const string PanelSettingsResource = "DevConsolePanelSettings";
        private const string ThemeStyleSheetResource = "DevConsoleTheme";
        private const string ConsoleStyleSheetResource = "DevConsoleStyle";
        private const string ConsoleFontResource = "Fonts/JetBrainsMono-Regular";
        private const float WindowHeightPercent = 50f;
        private const int MaxEntries = 1500;
        private const int TrimBatch = 200;
        private const int FontSize = 16;
        private const int InputFontSize = 18;
        private static readonly Color ConsoleBackground = new Color(0.10f, 0.12f, 0.15f, 0.9f);
        private static readonly Color AccentColor = new Color(0.45f, 0.85f, 1f);
        private static readonly bool InputOnBottom = true;

        // ── Types ─────────────────────────────────────────────────────────────
        private readonly struct Entry
        {
            public readonly DateTime Time;
            public readonly CONSOLE_LINE_KIND Kind;
            public readonly string Text;

            public Entry(DateTime time, CONSOLE_LINE_KIND kind, string text)
            {
                Time = time;
                Kind = kind;
                Text = text;
            }
        }

        // ── State ─────────────────────────────────────────────────────────────
        private static DevConsole instance;
        private static readonly ConcurrentQueue<Entry> pending = new ConcurrentQueue<Entry>();
        private static volatile bool showStackTraces;

        private readonly List<Entry> entries = new List<Entry>();
        private int builtCount; // entries already turned into UI rows
        private int bottomFrames; // frames left to keep the view pinned to the newest line

        private readonly List<string> history = new List<string>();
        private int historyIndex;

        private bool isOpen;
        private bool openWhenReady;
        private INPUTACTION_MAP previousMap = INPUTACTION_MAP.Empty;
        private bool previousSendNavigation = true;
        private CursorLockMode previousLockState;
        private bool previousCursorVisible;

        // UI
        private UIDocument document;
        private VisualElement window;
        private ScrollView scroll;
        private TextField inputField;

        // ── Public static API ─────────────────────────────────────────────────
        public static bool IsOpen => instance != null && instance.isOpen;

        /// <summary>Thread-safe. Prints a line to the console.</summary>
        public static void Print(string message, CONSOLE_LINE_KIND kind = CONSOLE_LINE_KIND.Output) =>
            pending.Enqueue(new Entry(DateTime.Now, kind, message ?? string.Empty));

        public static void PrintWarning(string message) => Print(message, CONSOLE_LINE_KIND.Warning);
        public static void PrintError(string message) => Print(message, CONSOLE_LINE_KIND.Error);

        /// <summary>Closes the console (useful for commands that should hand control back to the game).</summary>
        public static void Hide()
        {
            if (instance != null && instance.isOpen) instance.Close();
        }

        /// <summary>Removes every line from the console.</summary>
        public static void Clear() => instance?.ClearInternal();

        /// <summary>Toggles printing stack traces under error lines. Returns the new value.</summary>
        internal static bool ToggleStackTraces()
        {
            showStackTraces = !showStackTraces;
            return showStackTraces;
        }

        /// <summary>The map gameplay will have once the console closes (the console itself forces Empty while open).</summary>
        internal static INPUTACTION_MAP GameplayMap
        {
            get
            {
                if (instance != null && instance.isOpen) return instance.previousMap;
                return InputManager.Instance != null ? InputManager.Instance.CurrentMap : INPUTACTION_MAP.Empty;
            }
        }

        /// <summary>Writes the whole log to a text file in persistentDataPath and returns the path.</summary>
        internal static string SaveToFile()
        {
            if (instance == null) return null;
            instance.DrainPending();

            var sb = new StringBuilder();
            foreach (Entry e in instance.entries)
                sb.Append(e.Time.ToString("HH:mm:ss.fff")).Append(" [").Append(e.Kind).Append("] ").AppendLine(e.Text);

            string path = Path.Combine(Application.persistentDataPath, $"console-{DateTime.Now:yyyyMMdd-HHmmss}.log");
            File.WriteAllText(path, sb.ToString());
            return path;
        }

        // ── Bootstrap / lifecycle ─────────────────────────────────────────────
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null) return;

            var go = new GameObject("[DevConsole]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<DevConsole>();
        }

        private void Awake()
        {
            // Capture logs from the very first frame (also receives logs from other threads).
            Application.logMessageReceivedThreaded += OnLogReceived;

            CommandRegistry.Build();

            document = gameObject.AddComponent<UIDocument>();
            document.enabled = false;
            document.panelSettings = LoadOrCreatePanelSettings();
            document.sortingOrder = 32000;
            document.enabled = true;

            Print($"Dev console ready: {CommandRegistry.Count} commands. Type 'help'.", CONSOLE_LINE_KIND.Output);
        }

        // The UIDocument builds its root element on enable, so the tree is created one frame later.
        private void Start() => BuildUi();

        private void OnDestroy()
        {
            Application.logMessageReceivedThreaded -= OnLogReceived;

            if (instance == this) instance = null;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f4Key.wasPressedThisFrame)
                Toggle();

            DrainPending();
            SyncRows();

            if (!isOpen) return;

            KeepGameplayInputBlocked();

            if (bottomFrames > 0)
            {
                bottomFrames--;
                scroll.verticalScroller.value = scroll.verticalScroller.highValue;
            }
        }

        private static PanelSettings LoadOrCreatePanelSettings()
        {
            ThemeStyleSheet theme = Resources.Load<ThemeStyleSheet>(ThemeStyleSheetResource);
            if (theme == null)
                Debug.LogError($"[DevConsole] Missing Resources/{ThemeStyleSheetResource}.tss; UI Toolkit cannot render the console without a theme.");

            var asset = Resources.Load<PanelSettings>(PanelSettingsResource);
            if (asset != null)
            {
                PanelSettings settingsOverride = Instantiate(asset);
                settingsOverride.name = $"{asset.name} (runtime)";
                settingsOverride.sortingOrder = 32000f;
                settingsOverride.themeStyleSheet = theme;
                return settingsOverride;
            }

            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "DevConsolePanelSettings (runtime)";
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            settings.sortingOrder = 32000f; // above every game UI
            settings.themeStyleSheet = theme;
            return settings;
        }

        // ── Log intake ────────────────────────────────────────────────────────
        private static void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            CONSOLE_LINE_KIND kind = type == LogType.Log ? CONSOLE_LINE_KIND.Log
                : type == LogType.Warning ? CONSOLE_LINE_KIND.Warning
                : CONSOLE_LINE_KIND.Error;

            string text = condition;
            if (showStackTraces && kind == CONSOLE_LINE_KIND.Error && !string.IsNullOrEmpty(stackTrace))
                text += "\n" + stackTrace.TrimEnd();

            pending.Enqueue(new Entry(DateTime.Now, kind, text));
        }

        private void DrainPending()
        {
            while (pending.TryDequeue(out Entry entry)) entries.Add(entry);
        }

        private void ClearInternal()
        {
            while (pending.TryDequeue(out _)) { }
            entries.Clear();
            builtCount = 0;
            scroll?.contentContainer.Clear();
        }

        // ── UI construction ───────────────────────────────────────────────────
        private void BuildUi()
        {
            VisualElement root = document.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;

            StyleSheet consoleStyle = Resources.Load<StyleSheet>(ConsoleStyleSheetResource);
            if (consoleStyle == null)
                Debug.LogError($"[DevConsole] Could not load the console style at Resources/{ConsoleStyleSheetResource}.uss.");
            else
                root.styleSheets.Add(consoleStyle);

            Font font = Resources.Load<Font>(ConsoleFontResource);
            if (font == null)
            {
                Debug.LogError($"[DevConsole] Could not load the console font at Resources/{ConsoleFontResource}.");
            }
            else
            {
                root.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(font));
                root.style.unityFontStyleAndWeight = FontStyle.Normal;
            }

            window = new VisualElement { name = "dev-console" };
            window.style.position = Position.Absolute;
            window.style.left = 0f;
            window.style.right = 0f;
            window.style.top = 0f;
            window.style.width = Length.Percent(100f);
            window.style.height = Length.Percent(WindowHeightPercent);
            window.style.flexDirection = FlexDirection.Column;
            window.style.overflow = Overflow.Hidden;
            window.style.backgroundColor = ConsoleBackground;
            window.style.display = DisplayStyle.None;

            scroll = new ScrollView(ScrollViewMode.Vertical) { name = "dev-console-log" };
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.mouseWheelScrollSize = 60f;
            scroll.style.flexGrow = 1f;
            scroll.style.flexShrink = 1f;
            scroll.style.flexBasis = 0f;
            scroll.style.minHeight = 0f;
            scroll.style.paddingLeft = 8f;
            scroll.style.paddingRight = 8f;
            scroll.style.paddingTop = 4f;
            scroll.style.paddingBottom = 4f;

            VisualElement inputRow = BuildInputRow();

            if (InputOnBottom)
            {
                window.Add(scroll);
                window.Add(inputRow);
            }
            else
            {
                window.Add(inputRow);
                window.Add(scroll);
            }

            root.Add(window);

            // Rows for everything logged before the UI existed.
            builtCount = 0;
            SyncRows();

            if (openWhenReady)
            {
                openWhenReady = false;
                Open();
            }
        }

        private VisualElement BuildInputRow()
        {
            var row = new VisualElement { name = "dev-console-input-row" };
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.flexShrink = 0f;
            row.style.height = 56f;
            row.style.minHeight = 56f;
            row.style.paddingLeft = 8f;
            row.style.paddingRight = 8f;
            row.style.backgroundColor = ConsoleBackground;
            row.style.borderTopWidth = 1f;
            row.style.borderTopColor = new Color(0.45f, 0.55f, 0.65f, 0.9f);

            var prompt = new Label(">");
            StyleLabel(prompt, AccentColor);
            prompt.style.marginRight = 8f;
            row.Add(prompt);

            inputField = new TextField { name = "dev-console-input" };
            inputField.style.flexGrow = 1f;
            inputField.style.flexShrink = 1f;
            inputField.style.flexBasis = 0f;
            inputField.style.height = 44f;
            inputField.style.minHeight = 44f;
            inputField.style.fontSize = InputFontSize;
            inputField.style.unityFontStyleAndWeight = FontStyle.Normal;
            inputField.style.backgroundColor = ConsoleBackground;
            inputField.style.borderTopWidth = 0f;
            inputField.style.borderBottomWidth = 0f;
            inputField.style.borderLeftWidth = 0f;
            inputField.style.borderRightWidth = 0f;
            inputField.style.marginLeft = 0f;
            inputField.style.marginRight = 0f;
            inputField.style.marginTop = 0f;
            inputField.style.marginBottom = 0f;
            // Unity 6 exposes text selection through ITextSelection on the element itself.
            ITextSelection fieldSelection = inputField as ITextSelection;
            if (fieldSelection != null)
            {
                fieldSelection.selectAllOnFocus = false;
                fieldSelection.selectAllOnMouseUp = false;
            }

            // The editable area is a child element; style it directly so no theme is required.
            VisualElement textInput = inputField.Q("unity-text-input");
            if (textInput != null)
            {
                textInput.style.height = 44f;
                textInput.style.minHeight = 44f;
                textInput.style.backgroundColor = ConsoleBackground;
                textInput.style.color = Color.white;
                textInput.style.fontSize = InputFontSize;
                textInput.style.unityFontStyleAndWeight = FontStyle.Normal;
                textInput.style.borderTopWidth = 0f;
                textInput.style.borderBottomWidth = 0f;
                textInput.style.borderLeftWidth = 0f;
                textInput.style.borderRightWidth = 0f;
                textInput.style.paddingLeft = 0f;
            }

            // TrickleDown: intercept Enter / Tab / arrows before the text field handles them.
            inputField.RegisterCallback<KeyDownEvent>(OnInputKeyDown, TrickleDown.TrickleDown);
            row.Add(inputField);

            return row;
        }

        private static void StyleLabel(Label label, Color color)
        {
            label.style.color = color;
            label.style.fontSize = FontSize;
            label.style.marginLeft = 0f;
            label.style.marginRight = 0f;
            label.style.marginTop = 0f;
            label.style.marginBottom = 0f;
            label.style.paddingLeft = 0f;
            label.style.paddingRight = 0f;
            label.style.paddingTop = 0f;
            label.style.paddingBottom = 0f;
            label.style.unityTextAlign = TextAnchor.UpperLeft;
        }

        // ── Rows ──────────────────────────────────────────────────────────────
        /// <summary>Turns new entries into UI rows and trims the oldest ones.</summary>
        private void SyncRows()
        {
            if (scroll == null || builtCount >= entries.Count) return;

            VisualElement content = scroll.contentContainer;
            bool wasAtBottom = IsAtBottom();

            for (; builtCount < entries.Count; builtCount++) content.Add(CreateRow(entries[builtCount]));

            if (entries.Count > MaxEntries + TrimBatch)
            {
                entries.RemoveRange(0, TrimBatch);
                builtCount -= TrimBatch;
                for (int i = 0; i < TrimBatch; i++) content.RemoveAt(0);
            }

            // Follow new lines only if the user was already reading the bottom.
            if (wasAtBottom) bottomFrames = 3;
        }

        private bool IsAtBottom()
        {
            Scroller scroller = scroll.verticalScroller;
            return scroller.value >= scroller.highValue - 6f;
        }

        private static VisualElement CreateRow(Entry entry)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexShrink = 0f;
            row.style.alignItems = Align.FlexStart;
            row.style.width = Length.Percent(100f);
            row.style.paddingTop = 3f;
            row.style.paddingBottom = 3f;

            var stamp = new Label(entry.Time.ToString("HH:mm:ss"));
            StyleLabel(stamp, new Color(0.55f, 0.6f, 0.65f));
            stamp.style.width = 80f;
            stamp.style.flexShrink = 0f;
            stamp.style.marginRight = 14f;

            var message = new Label(entry.Text.Replace("\r", string.Empty));
            StyleLabel(message, ColorFor(entry.Kind));
            message.style.flexGrow = 1f;
            message.style.flexShrink = 1f;
            message.style.flexBasis = 0f;
            message.style.whiteSpace = WhiteSpace.Normal;
            if (message is ITextSelection messageSelection) messageSelection.isSelectable = true;

            row.Add(stamp);
            row.Add(message);
            return row;
        }

        private static Color ColorFor(CONSOLE_LINE_KIND kind)
        {
            switch (kind)
            {
                case CONSOLE_LINE_KIND.Warning: return new Color(1f, 0.85f, 0.35f);
                case CONSOLE_LINE_KIND.Error:   return new Color(1f, 0.4f, 0.4f);
                case CONSOLE_LINE_KIND.Command: return AccentColor;
                case CONSOLE_LINE_KIND.Output:  return new Color(0.6f, 0.95f, 0.65f);
                default:                        return new Color(0.9f, 0.9f, 0.9f);
            }
        }

        // ── Open / close ──────────────────────────────────────────────────────
        private void Toggle()
        {
            if (window == null)
            {
                openWhenReady = !openWhenReady;
                return;
            }

            if (isOpen) Close();
            else Open();
        }

        private void Open()
        {
            isOpen = true;
            historyIndex = history.Count;

            InputManager inputManager = InputManager.Instance;
            if (inputManager != null)
            {
                previousMap = inputManager.CurrentMap;
                inputManager.ChangeActionMap(INPUTACTION_MAP.Empty);
            }

            // Otherwise Enter would also "submit" whatever UI button is currently selected.
            if (EventSystem.current != null)
            {
                previousSendNavigation = EventSystem.current.sendNavigationEvents;
                EventSystem.current.sendNavigationEvents = false;
            }

            previousLockState = UnityEngine.Cursor.lockState;
            previousCursorVisible = UnityEngine.Cursor.visible;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;

            window.style.display = DisplayStyle.Flex;
            inputField.value = string.Empty;
            bottomFrames = 3;
            inputField.schedule.Execute(() => inputField.Focus());
        }

        private void Close()
        {
            isOpen = false;
            window.style.display = DisplayStyle.None;
            inputField.Blur();

            // Only restore if nobody else changed the map in the meantime.
            InputManager inputManager = InputManager.Instance;
            if (inputManager != null && inputManager.CurrentMap == INPUTACTION_MAP.Empty)
                inputManager.ChangeActionMap(previousMap);

            if (EventSystem.current != null)
                EventSystem.current.sendNavigationEvents = previousSendNavigation;

            UnityEngine.Cursor.lockState = previousLockState;
            UnityEngine.Cursor.visible = previousCursorVisible;
        }

        /// <summary>
        /// Scene loads or commands can switch the input map while the console is open.
        /// Remember the requested map and push Empty again so typing never reaches gameplay.
        /// </summary>
        private void KeepGameplayInputBlocked()
        {
            InputManager inputManager = InputManager.Instance;
            if (inputManager == null || inputManager.CurrentMap == INPUTACTION_MAP.Empty) return;

            previousMap = inputManager.CurrentMap;
            inputManager.ChangeActionMap(INPUTACTION_MAP.Empty);
        }

        // ── Command bar logic ─────────────────────────────────────────────────
        private void OnInputKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    Submit();
                    Consume(evt);
                    break;
                case KeyCode.UpArrow:
                    NavigateHistory(-1);
                    Consume(evt);
                    break;
                case KeyCode.DownArrow:
                    NavigateHistory(1);
                    Consume(evt);
                    break;
                case KeyCode.Tab:
                    Autocomplete();
                    Consume(evt);
                    break;
                case KeyCode.PageUp:
                    scroll.verticalScroller.value -= scroll.layout.height * 0.9f;
                    Consume(evt);
                    break;
                case KeyCode.PageDown:
                    scroll.verticalScroller.value += scroll.layout.height * 0.9f;
                    Consume(evt);
                    break;
                case KeyCode.Escape:
                    Close();
                    Consume(evt);
                    break;
            }
        }

        private static void Consume(KeyDownEvent evt)
        {
            evt.StopImmediatePropagation();
            evt.PreventDefault();
        }

        private void SetInput(string text)
        {
            inputField.value = text;
            // Cursor to the end once the value has been applied.
            inputField.schedule.Execute(() => (inputField as ITextSelection)?.SelectRange(text.Length, text.Length));
        }

        private void Submit()
        {
            string line = inputField.value.Trim();
            inputField.value = string.Empty;
            if (line.Length == 0) return;

            if (history.Count == 0 || history[history.Count - 1] != line) history.Add(line);
            historyIndex = history.Count;

            bottomFrames = 3;
            Print("> " + line, CONSOLE_LINE_KIND.Command);
            CommandRegistry.Execute(line);
        }

        private void NavigateHistory(int direction)
        {
            if (history.Count == 0) return;

            historyIndex = Mathf.Clamp(historyIndex + direction, 0, history.Count);
            SetInput(historyIndex == history.Count ? string.Empty : history[historyIndex]);
        }

        private void Autocomplete()
        {
            string typed = inputField.value.TrimStart('/');
            if (typed.Contains(" ")) return; // only command names are completed

            List<string> matches = CommandRegistry.Match(typed);
            if (matches.Count == 0) return;

            if (matches.Count == 1)
            {
                SetInput(matches[0] + " ");
            }
            else
            {
                Print(string.Join("   ", matches), CONSOLE_LINE_KIND.Output);
                SetInput(CommonPrefix(matches));
            }
        }

        private static string CommonPrefix(List<string> values)
        {
            string prefix = values[0];
            foreach (string value in values)
            {
                int n = 0;
                while (n < prefix.Length && n < value.Length &&
                       char.ToLowerInvariant(prefix[n]) == char.ToLowerInvariant(value[n])) n++;
                prefix = prefix.Substring(0, n);
            }
            return prefix;
        }
    }
}
#endif