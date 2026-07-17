#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PlantsPathCo.DialogueSystem.Editor
{
    [CustomEditor(typeof(DialogueManager))]
    public class DialogueManagerEditor : UnityEditor.Editor
    {
        // Serialized properties
        private SerializedProperty displayModeProp;
        private SerializedProperty dialoguePanelProp;
        private SerializedProperty audioSourceProp;
        private SerializedProperty choiceInputsProp;
        private SerializedProperty onConversationStartProp;
        private SerializedProperty onConversationEndProp;
        private SerializedProperty onLineDisplayedProp;
        private SerializedProperty onChoiceSelectedProp;

        // Runtime-only properties, shown read-only during Play Mode
        private SerializedProperty currentConversationProp;
        private SerializedProperty lineIndexProp;

        // Foldout state (shared across selections, resets on recompile)
        private static bool showGeneral = true;
        private static bool showUI = true;
        private static bool showAudio = true;
        private static bool showInput = true;
        private static bool showEvents = false;

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle statusBoxStyle;

        private void OnEnable()
        {
            displayModeProp = serializedObject.FindProperty("displayMode");
            dialoguePanelProp = serializedObject.FindProperty("dialoguePanel");
            audioSourceProp = serializedObject.FindProperty("audioSource");
            choiceInputsProp = serializedObject.FindProperty("choiceInputs");
            onConversationStartProp = serializedObject.FindProperty("onConversationStart");
            onConversationEndProp = serializedObject.FindProperty("onConversationEnd");
            onLineDisplayedProp = serializedObject.FindProperty("onLineDisplayed");
            onChoiceSelectedProp = serializedObject.FindProperty("onChoiceSelected");

            currentConversationProp = serializedObject.FindProperty("currentConversation");
            lineIndexProp = serializedObject.FindProperty("lineIndex");
        }

        private void InitStyles()
        {
            if (titleStyle != null) return;

            titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft
            };

            subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Italic,
                wordWrap = true
            };

            statusBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 6, 6)
            };
        }

        // Continuously repaint during Play Mode to show live runtime state
        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            InitStyles();
            serializedObject.Update();
            var manager = (DialogueManager)target;

            DrawHeader();
            DrawCreatePanelButton(manager);

            if (Application.isPlaying)
                DrawRuntimeStatus(manager);

            EditorGUILayout.Space(4);

            DrawSection("⚙  General", ref showGeneral, () =>
            {
                EditorGUILayout.PropertyField(displayModeProp, new GUIContent("Display Mode"));
            });

            // Get current display mode to conditionally show sections
            var displayMode = (DisplayMode)displayModeProp.enumValueIndex;

            // Show UI section only if TextOnly or Both
            if (displayMode == DisplayMode.TextOnly || displayMode == DisplayMode.Both)
            {
                DrawSection("🖼  UI Panel (optional)", ref showUI, () =>
                {
                    EditorGUILayout.PropertyField(dialoguePanelProp, true);
                    DrawUIWarnings();
                });
            }

            // Show Audio section only if VoiceOnly or Both
            if (displayMode == DisplayMode.VoiceOnly || displayMode == DisplayMode.Both)
            {
                DrawSection("♪  Audio (optional)", ref showAudio, () =>
                {
                    EditorGUILayout.PropertyField(audioSourceProp, new GUIContent("Audio Source"));

                    if (audioSourceProp.objectReferenceValue == null)
                    {
                        EditorGUILayout.HelpBox(
                            "Current display mode can play voice lines, but no Audio Source is assigned.",
                            MessageType.Warning);
                    }
                });
            }

            DrawSection("⌨  Input Actions (up to 4)", ref showInput, () =>
            {
                EditorGUILayout.HelpBox(
                    "One InputActionReference per answer option, in order.",
                    MessageType.None);
                EditorGUILayout.PropertyField(choiceInputsProp, true);
            });

            DrawSection("⚡  Events", ref showEvents, () =>
            {
                EditorGUILayout.PropertyField(onConversationStartProp);
                EditorGUILayout.PropertyField(onConversationEndProp);
                EditorGUILayout.PropertyField(onLineDisplayedProp);
                EditorGUILayout.PropertyField(onChoiceSelectedProp);
            });

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCreatePanelButton(DialogueManager manager)
        {
            EditorGUILayout.Space(2);
            if (GUILayout.Button("＋  Create Dialogue Panel In Scene", GUILayout.Height(24)))
            {
                CreateDialoguePanel(manager);
            }
            EditorGUILayout.Space(4);
        }

        // Appears when right-clicking the DialogueManager component header (⋮ menu) in the Inspector
        [MenuItem("CONTEXT/DialogueManager/Create Dialogue Panel In Scene")]
        private static void CreateDialoguePanelContextMenu(MenuCommand command)
        {
            CreateDialoguePanel((DialogueManager)command.context);
        }

        // Builds a basic, ready-to-use dialogue UI hierarchy and wires it into the manager's DialoguePanel struct
        private static void CreateDialoguePanel(DialogueManager manager)
        {
            var so = new SerializedObject(manager);
            var panelProp = so.FindProperty("dialoguePanel");
            var existingRoot = panelProp.FindPropertyRelative("panelRoot").objectReferenceValue;

            if (existingRoot != null)
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Dialogue Panel Already Assigned",
                    "This DialogueManager already has a Dialogue Panel assigned. Create a new one anyway?",
                    "Create New", "Cancel");
                if (!replace) return;
            }

            EnsureEventSystem();
            Canvas canvas = FindOrCreateCanvas();

            GameObject panelRoot = CreateUIObject("DialoguePanel", canvas.transform);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.05f);
            panelRect.anchorMax = new Vector2(0.9f, 0.35f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImage = panelRoot.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.75f);

            GameObject speakerNameGO = CreateUIObject("SpeakerName", panelRoot.transform);
            var speakerRect = speakerNameGO.GetComponent<RectTransform>();
            speakerRect.anchorMin = new Vector2(0f, 0.8f);
            speakerRect.anchorMax = new Vector2(1f, 1f);
            speakerRect.offsetMin = new Vector2(20f, 0f);
            speakerRect.offsetMax = new Vector2(-20f, 0f);
            var speakerText = speakerNameGO.AddComponent<TextMeshProUGUI>();
            speakerText.text = "Speaker Name";
            speakerText.fontSize = 24;
            speakerText.fontStyle = FontStyles.Bold;
            speakerText.color = Color.white;

            GameObject portraitGO = CreateUIObject("Portrait", panelRoot.transform);
            var portraitRect = portraitGO.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0f, 0f);
            portraitRect.anchorMax = new Vector2(0.15f, 0.8f);
            portraitRect.offsetMin = new Vector2(10f, 10f);
            portraitRect.offsetMax = new Vector2(-5f, -5f);
            var portraitImage = portraitGO.AddComponent<Image>();
            portraitImage.color = new Color(1f, 1f, 1f, 0.15f);

            GameObject dialogueTextGO = CreateUIObject("DialogueText", panelRoot.transform);
            var dialogueTextRect = dialogueTextGO.GetComponent<RectTransform>();
            dialogueTextRect.anchorMin = new Vector2(0.17f, 0.15f);
            dialogueTextRect.anchorMax = new Vector2(1f, 0.78f);
            dialogueTextRect.offsetMin = new Vector2(10f, 5f);
            dialogueTextRect.offsetMax = new Vector2(-20f, -5f);
            var dialogueText = dialogueTextGO.AddComponent<TextMeshProUGUI>();
            dialogueText.text = "Dialogue text goes here...";
            dialogueText.fontSize = 20;
            dialogueText.color = Color.white;
            dialogueText.textWrappingMode = TextWrappingModes.Normal;

            GameObject answerPanelGO = CreateUIObject("AnswerPanel", panelRoot.transform);
            var answerPanelRect = answerPanelGO.GetComponent<RectTransform>();
            answerPanelRect.anchorMin = new Vector2(0.17f, 0f);
            answerPanelRect.anchorMax = new Vector2(1f, 0.15f);
            answerPanelRect.offsetMin = new Vector2(10f, 5f);
            answerPanelRect.offsetMax = new Vector2(-20f, 0f);
            var answerLayout = answerPanelGO.AddComponent<HorizontalLayoutGroup>();
            answerLayout.spacing = 10f;
            answerLayout.childForceExpandWidth = true;
            answerLayout.childForceExpandHeight = true;

            var answerLabels = new TextMeshProUGUI[4];
            for (int i = 0; i < answerLabels.Length; i++)
            {
                GameObject answerGO = CreateUIObject($"Answer_{i}", answerPanelGO.transform);
                var answerImage = answerGO.AddComponent<Image>();
                answerImage.color = new Color(1f, 1f, 1f, 0.1f);

                GameObject answerTextGO = CreateUIObject("Text", answerGO.transform);
                var answerTextRect = answerTextGO.GetComponent<RectTransform>();
                answerTextRect.anchorMin = Vector2.zero;
                answerTextRect.anchorMax = Vector2.one;
                answerTextRect.offsetMin = Vector2.zero;
                answerTextRect.offsetMax = Vector2.zero;

                var answerText = answerTextGO.AddComponent<TextMeshProUGUI>();
                answerText.text = $"Answer {i + 1}";
                answerText.fontSize = 16;
                answerText.alignment = TextAlignmentOptions.Center;
                answerText.color = Color.white;

                answerLabels[i] = answerText;
            }

            answerPanelGO.SetActive(false);
            Undo.RegisterCreatedObjectUndo(panelRoot, "Create Dialogue Panel");

            // Wire references into the manager's DialoguePanel struct
            panelProp.FindPropertyRelative("panelRoot").objectReferenceValue = panelRoot;
            panelProp.FindPropertyRelative("dialogueText").objectReferenceValue = dialogueText;
            panelProp.FindPropertyRelative("answerPanel").objectReferenceValue = answerPanelGO;
            panelProp.FindPropertyRelative("portraitImage").objectReferenceValue = portraitImage;
            panelProp.FindPropertyRelative("speakerNameText").objectReferenceValue = speakerText;

            var answerLabelsProp = panelProp.FindPropertyRelative("answerLabels");
            answerLabelsProp.arraySize = answerLabels.Length;
            for (int i = 0; i < answerLabels.Length; i++)
                answerLabelsProp.GetArrayElementAtIndex(i).objectReferenceValue = answerLabels[i];

            so.ApplyModifiedProperties();

            // Hidden by default; DialogueManager activates it when a conversation starts
            panelRoot.SetActive(false);

            Selection.activeGameObject = panelRoot;
            EditorUtility.SetDirty(manager);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }

        private static Canvas FindOrCreateCanvas()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null) return canvas;

            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");

            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            var eventSystemGO = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystemGO, "Create EventSystem");
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        // ---------- Visual Blocks ----------

        private void DrawHeader()
        {
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("🗨", new GUIStyle(titleStyle) { fontSize = 20 }, GUILayout.Width(24));
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label("Dialogue Manager", titleStyle);
                    GUILayout.Label("Universal and flexible dialogue system", subtitleStyle);
                }
            }
            EditorGUILayout.Space(4);
            DrawSeparator();
        }

        private void DrawSeparator()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.25f));
            EditorGUILayout.Space(2);
        }

        private void DrawRuntimeStatus(DialogueManager manager)
        {
            bool talking = manager.IsTalking;

            Color prevColor = GUI.backgroundColor;
            GUI.backgroundColor = talking ? new Color(0.45f, 0.85f, 0.45f) : new Color(0.6f, 0.6f, 0.6f);

            string status = talking ? "▶ Playing" : "■ Idle";
            EditorGUILayout.LabelField(status, statusBoxStyle);
            GUI.backgroundColor = prevColor;

            // Read-only view of runtime state: visible but not editable while playing
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(currentConversationProp, new GUIContent("Current Conversation"));
                EditorGUILayout.PropertyField(lineIndexProp, new GUIContent("Line Index"));
            }

            EditorGUILayout.Space(4);
        }

        private void DrawUIWarnings()
        {
            var panelRoot = dialoguePanelProp.FindPropertyRelative("panelRoot");
            var dialogueText = dialoguePanelProp.FindPropertyRelative("dialogueText");
            var answerPanel = dialoguePanelProp.FindPropertyRelative("answerPanel");
            var answerLabels = dialoguePanelProp.FindPropertyRelative("answerLabels");

            if (panelRoot != null && panelRoot.objectReferenceValue == null &&
                dialogueText != null && dialogueText.objectReferenceValue != null)
            {
                EditorGUILayout.HelpBox(
                    "'Dialogue Text' is assigned but 'Panel Root' is not. The panel will never activate.",
                    MessageType.Warning);
            }

            if (answerPanel != null && answerPanel.objectReferenceValue != null &&
                (answerLabels == null || answerLabels.arraySize == 0))
            {
                EditorGUILayout.HelpBox(
                    "'Answer Panel' is assigned but 'Answer Labels' is empty. Answers won't be displayed.",
                    MessageType.Warning);
            }
        }

        // Manual foldout: avoids "You can't nest Foldout Headers" error
        // that occurs with BeginFoldoutHeaderGroup when more than one is active.
        private void DrawSection(string label, ref bool foldout, System.Action drawContent)
        {
            EditorGUILayout.Space(2);

            // Draw header manually with a colored Rect
            Rect headerRect = EditorGUILayout.GetControlRect(false, 22f);
            EditorGUI.DrawRect(headerRect, new Color(0.18f, 0.18f, 0.18f, 0.4f));

            // Foldout triangle + label inside the rect
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                padding = new RectOffset(18, 4, 3, 0)
            };
            foldout = EditorGUI.Foldout(headerRect, foldout, label, true, headerStyle);

            if (foldout)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.Space(2);
                    drawContent();
                    EditorGUILayout.Space(2);
                }
            }
        }
    }
}
#endif