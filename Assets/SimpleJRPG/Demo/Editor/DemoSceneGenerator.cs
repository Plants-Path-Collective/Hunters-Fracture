using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using TMPro;

namespace SimpleJRPG.Demo.Editor
{
    public static class DemoSceneGenerator
    {
        // ── DQ-inspired dark palette ──
        private static readonly Color BgBlack = new Color(0.06f, 0.05f, 0.08f);
        private static readonly Color WindowBg = new Color(0.05f, 0.04f, 0.07f);
        private static readonly Color BorderPink = new Color(0.831f, 0.510f, 0.612f);
        private static readonly Color TextWhite = Color.white;
        private static readonly Color TextGold = new Color(1f, 0.85f, 0.3f);
        private static readonly Color BarGreen = new Color(0.3f, 0.8f, 0.35f);
        private static readonly Color BarBlue = new Color(0.35f, 0.5f, 0.9f);
        private static readonly Color BarBgDark = new Color(0.15f, 0.13f, 0.18f);
        private static readonly Color PortraitBg = new Color(0.2f, 0.18f, 0.22f);
        private static readonly Color BtnHighlight = new Color(0.15f, 0.12f, 0.18f);
        private static readonly Color BtnPressed = new Color(0.6f, 0.3f, 0.45f);

        // ── Menu Items ──

        // [MenuItem("Window/Living Failure/Simple JRPG/Generate Demo Hub")]
        public static void GenerateDemoHub()
        {
            // Canvas
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // EventSystem
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
                var inputSystemType = System.Type.GetType(
                    "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputSystemType != null)
                    eventSystem.AddComponent(inputSystemType);
                else
                    eventSystem.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
            }

            var root = canvasGO.transform;

            // ── Full-screen background ──
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(root, false);
            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            bgGO.AddComponent<Image>().color = BgBlack;

            // ── Center panel ──
            var panelInner = CreateBorderedWindow(root, "HubPanel",
                new Vector2(0.25f, 0.1f), new Vector2(0.75f, 0.9f));

            var panelVLG = panelInner.gameObject.AddComponent<VerticalLayoutGroup>();
            panelVLG.padding = new RectOffset(30, 30, 30, 30);
            panelVLG.spacing = 12;
            panelVLG.childControlWidth = true;
            panelVLG.childControlHeight = true;
            panelVLG.childForceExpandWidth = true;
            panelVLG.childForceExpandHeight = false;
            panelVLG.childAlignment = TextAnchor.UpperCenter;

            // Title
            var title = CreateText(panelInner, "Title", "Simple JRPG", 60);
            title.color = TextGold;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            var titleLE = title.gameObject.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 80;

            // Subtitle
            var subtitle = CreateText(panelInner, "Subtitle", "Demo Hub", 32);
            subtitle.color = BorderPink;
            subtitle.alignment = TextAlignmentOptions.Center;
            var subtitleLE = subtitle.gameObject.AddComponent<LayoutElement>();
            subtitleLE.preferredHeight = 45;

            // Spacer
            var spacer1 = new GameObject("Spacer");
            spacer1.transform.SetParent(panelInner, false);
            var spacer1LE = spacer1.AddComponent<LayoutElement>();
            spacer1LE.preferredHeight = 20;

            // Demo buttons
            var btnClassic = CreateCommandButton(panelInner, "Classic Turn (DQ3 Style)");
            btnClassic.gameObject.GetComponent<LayoutElement>().preferredHeight = 70;

            var btnATB = CreateCommandButton(panelInner, "ATB Turn (FF7 Style)");
            btnATB.gameObject.GetComponent<LayoutElement>().preferredHeight = 70;

            var btnTimeline = CreateCommandButton(panelInner, "Timeline Turn (FFX Style)");
            btnTimeline.gameObject.GetComponent<LayoutElement>().preferredHeight = 70;

            var btnPressTurn = CreateCommandButton(panelInner, "Press Turn (SMT Style)");
            btnPressTurn.gameObject.GetComponent<LayoutElement>().preferredHeight = 70;

            var btnActionPoint = CreateCommandButton(panelInner, "Action Point Turn");
            btnActionPoint.gameObject.GetComponent<LayoutElement>().preferredHeight = 70;

            // ═══════════════════════════════════════
            // WIRE UP
            // ═══════════════════════════════════════

            var hub = canvasGO.AddComponent<DemoHub>();
            hub.btnClassicTurn = btnClassic;
            hub.btnATBTurn = btnATB;
            hub.btnTimelineTurn = btnTimeline;
            hub.btnPressTurn = btnPressTurn;
            hub.btnActionPoint = btnActionPoint;

            Debug.Log("SimpleJRPG: Generated Demo Hub. Assign scene names in the DemoHub component.");
        }

        // [MenuItem("Window/Living Failure/Simple JRPG/Generators/Generate Classic Turn Demo")]
        public static void GenerateClassicTurnDemo()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = BgBlack;
            }

            // Canvas
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // EventSystem
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
                var inputSystemType = System.Type.GetType(
                    "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputSystemType != null)
                    eventSystem.AddComponent(inputSystemType);
                else
                    eventSystem.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
            }

            var root = canvasGO.transform;

            // ═══════════════════════════════════════
            // DQ3 LAYOUT
            // ═══════════════════════════════════════
            //
            //  ┌─────────────────────────────────────────┐
            //  │  [Blob]  [Imp]  [Wyvern]    ENEMY PANEL │
            //  ├───────┬─────────────────────┬───────────┤
            //  │ FIGHT │                     │ [Hero]    │
            //  │ HEAL  │   MESSAGE LOG       │  HP ████  │
            //  │REVIVE │   (scroll+bar)      │  MP ██    │
            //  │ FLEE  │                     │ [Warrior] │
            //  │       │                     │  HP ████  │
            //  │       │                     │  ...      │
            //  └───────┴─────────────────────┴───────────┘

            // ── Enemy Panel (top) ──
            var enemyInner = CreateBorderedWindow(root, "EnemyPanel",
                new Vector2(0.03f, 0.75f), new Vector2(0.97f, 0.97f));

            var enemyHLG = enemyInner.gameObject.AddComponent<HorizontalLayoutGroup>();
            enemyHLG.padding = new RectOffset(20, 20, 10, 10);
            enemyHLG.spacing = 40;
            enemyHLG.childControlWidth = true;
            enemyHLG.childControlHeight = true;
            enemyHLG.childForceExpandWidth = false;
            enemyHLG.childForceExpandHeight = false;
            enemyHLG.childAlignment = TextAnchor.MiddleCenter;

            var enemyPortraits = new Image[3];
            var enemyNameTexts = new TextMeshProUGUI[3];
            var enemyHPBars = new Slider[3];
            var enemyHPTexts = new TextMeshProUGUI[3];
            var enemySelectMarks = new GameObject[3];

            for (int i = 0; i < 3; i++)
            {
                var card = CreateCharacterCard(enemyInner, $"Enemy_{i}", false);
                enemyPortraits[i] = card.portrait;
                enemyNameTexts[i] = card.nameText;
                enemyHPBars[i] = card.hpBar;
                enemyHPTexts[i] = card.hpText;
                enemySelectMarks[i] = card.selectMark;
            }

            // ── Command Panel (left) ──
            var cmdInner = CreateBorderedWindow(root, "CommandPanel",
                new Vector2(0.03f, 0.03f), new Vector2(0.15f, 0.73f));

            var cmdVLG = cmdInner.gameObject.AddComponent<VerticalLayoutGroup>();
            cmdVLG.padding = new RectOffset(8, 8, 15, 15);
            cmdVLG.spacing = 8;
            cmdVLG.childControlWidth = true;
            cmdVLG.childControlHeight = true;
            cmdVLG.childForceExpandWidth = true;
            cmdVLG.childForceExpandHeight = false;
            cmdVLG.childAlignment = TextAnchor.UpperCenter;

            var btnFight = CreateCommandButton(cmdInner, "FIGHT");
            var btnHeal = CreateCommandButton(cmdInner, "HEAL");
            var btnRevive = CreateCommandButton(cmdInner, "REVIVE");
            var btnDefend = CreateCommandButton(cmdInner, "DEFEND");
            var btnFlee = CreateCommandButton(cmdInner, "FLEE");

            // Target buttons (same panel, hidden until Fight is pressed)
            var targetButtons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                targetButtons[i] = CreateCommandButton(cmdInner, "---");
                targetButtons[i].gameObject.SetActive(false);
            }
            var btnBack = CreateCommandButton(cmdInner, "BACK");
            btnBack.gameObject.SetActive(false);

            // ── Message Log (center) ──
            var msgInner = CreateBorderedWindow(root, "MessagePanel",
                new Vector2(0.16f, 0.03f), new Vector2(0.68f, 0.73f));

            var msgScroll = CreateDemoMessageArea(msgInner, "Simple JRPG-Classic Battle Turn Demo Scene");

            // ── Party Panel (right) ──
            var partyInner = CreateBorderedWindow(root, "PartyPanel",
                new Vector2(0.69f, 0.03f), new Vector2(0.97f, 0.73f));

            var partyVLG = partyInner.gameObject.AddComponent<VerticalLayoutGroup>();
            partyVLG.padding = new RectOffset(10, 10, 10, 10);
            partyVLG.spacing = 6;
            partyVLG.childControlWidth = true;
            partyVLG.childControlHeight = true;
            partyVLG.childForceExpandWidth = false;
            partyVLG.childForceExpandHeight = false;

            var partyPortraits = new Image[4];
            var partyNameTexts = new TextMeshProUGUI[4];
            var partyHPBars = new Slider[4];
            var partyMPBars = new Slider[4];
            var partyHPTexts = new TextMeshProUGUI[4];
            var partyMPTexts = new TextMeshProUGUI[4];
            var partySelectMarks = new GameObject[4];

            for (int i = 0; i < 4; i++)
            {
                var card = CreateCharacterCard(partyInner, $"Party_{i}", true);
                partyPortraits[i] = card.portrait;
                partyNameTexts[i] = card.nameText;
                partyHPBars[i] = card.hpBar;
                partyMPBars[i] = card.mpBar;
                partyHPTexts[i] = card.hpText;
                partyMPTexts[i] = card.mpText;
                partySelectMarks[i] = card.selectMark;
            }

            // ═══════════════════════════════════════
            // WIRE UP
            // ═══════════════════════════════════════

            var demo = canvasGO.AddComponent<ClassicTurnDemo>();

            demo.messageContent = msgScroll.content;
            demo.messageScrollRect = msgScroll;

            demo.enemyPortraits = enemyPortraits;
            demo.enemyNameTexts = enemyNameTexts;
            demo.enemyHPBars = enemyHPBars;
            demo.enemyHPTexts = enemyHPTexts;

            demo.partyPortraits = partyPortraits;
            demo.partyNameTexts = partyNameTexts;
            demo.partyHPBars = partyHPBars;
            demo.partyMPBars = partyMPBars;
            demo.partyHPTexts = partyHPTexts;
            demo.partyMPTexts = partyMPTexts;

            demo.btnFight = btnFight;
            demo.btnHeal = btnHeal;
            demo.btnRevive = btnRevive;
            demo.btnDefend = btnDefend;
            demo.btnFlee = btnFlee;
            demo.commandPanel = cmdInner.gameObject;

            demo.targetButtons = targetButtons;
            demo.btnBack = btnBack;

            demo.partySelectMarks = partySelectMarks;
            demo.enemySelectMarks = enemySelectMarks;

            // ── Save ──
            string dir = "Assets/SimpleJRPG/Scenes";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            EditorSceneManager.SaveScene(scene, $"{dir}/ClassicTurnDemo.unity");
            AssetDatabase.Refresh();
            Debug.Log("SimpleJRPG: Generated Classic Turn Demo (DQ3 style)");
        }

        // [MenuItem("Window/Living Failure/Simple JRPG/Generators/Generate ATB Turn Demo")]
        public static void GenerateATBTurnDemo()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = BgBlack;
            }

            // Canvas
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // EventSystem
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
                var inputSystemType = System.Type.GetType(
                    "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputSystemType != null)
                    eventSystem.AddComponent(inputSystemType);
                else
                    eventSystem.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
            }

            var root = canvasGO.transform;
            var BarATB = new Color(1f, 0.75f, 0.2f);

            // ── Enemy Panel (top) ──
            var enemyInner = CreateBorderedWindow(root, "EnemyPanel",
                new Vector2(0.03f, 0.75f), new Vector2(0.97f, 0.97f));

            var enemyHLG = enemyInner.gameObject.AddComponent<HorizontalLayoutGroup>();
            enemyHLG.padding = new RectOffset(20, 20, 10, 10);
            enemyHLG.spacing = 40;
            enemyHLG.childControlWidth = true;
            enemyHLG.childControlHeight = true;
            enemyHLG.childForceExpandWidth = false;
            enemyHLG.childForceExpandHeight = false;
            enemyHLG.childAlignment = TextAnchor.MiddleCenter;

            var enemyPortraits = new Image[3];
            var enemyNameTexts = new TextMeshProUGUI[3];
            var enemyHPBars = new Slider[3];
            var enemyHPTexts = new TextMeshProUGUI[3];
            var enemySelectMarks = new GameObject[3];
            var enemyATBBars = new Slider[3];

            for (int i = 0; i < 3; i++)
            {
                var card = CreateCharacterCard(enemyInner, $"Enemy_{i}", false);
                enemyPortraits[i] = card.portrait;
                enemyNameTexts[i] = card.nameText;
                enemyHPBars[i] = card.hpBar;
                enemyHPTexts[i] = card.hpText;
                enemySelectMarks[i] = card.selectMark;

                // ATB label + bar in stats column
                var statsT = card.nameText.transform.parent;
                var enemyATBLabel = CreateText(statsT, "ATBLabel", "ATB", 14);
                var atbLabelLE = enemyATBLabel.gameObject.AddComponent<LayoutElement>();
                atbLabelLE.preferredHeight = 16;
                atbLabelLE.preferredWidth = 200;
                enemyATBBars[i] = CreateBar(statsT, "ATBBar", BarATB);
                var atbBarLE = enemyATBBars[i].gameObject.AddComponent<LayoutElement>();
                atbBarLE.preferredHeight = 14;
                atbBarLE.preferredWidth = 200;
                enemyATBBars[i].value = 0;

                // Bump heights to fit ATB label + bar
                statsT.GetComponent<LayoutElement>().preferredHeight = 110;
                card.portrait.transform.parent.GetComponent<LayoutElement>().preferredHeight = 130;
            }

            // ── Command Panel (left) ──
            var cmdInner = CreateBorderedWindow(root, "CommandPanel",
                new Vector2(0.03f, 0.03f), new Vector2(0.15f, 0.73f));

            var cmdVLG = cmdInner.gameObject.AddComponent<VerticalLayoutGroup>();
            cmdVLG.padding = new RectOffset(8, 8, 15, 15);
            cmdVLG.spacing = 8;
            cmdVLG.childControlWidth = true;
            cmdVLG.childControlHeight = true;
            cmdVLG.childForceExpandWidth = true;
            cmdVLG.childForceExpandHeight = false;
            cmdVLG.childAlignment = TextAnchor.UpperCenter;

            var btnFight = CreateCommandButton(cmdInner, "FIGHT");
            var btnHeal = CreateCommandButton(cmdInner, "HEAL");
            var btnDefend = CreateCommandButton(cmdInner, "DEFEND");
            var btnRevive = CreateCommandButton(cmdInner, "REVIVE");
            var btnFlee = CreateCommandButton(cmdInner, "FLEE");

            // Target buttons (same panel, hidden until Fight is pressed)
            var targetButtons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                targetButtons[i] = CreateCommandButton(cmdInner, "---");
                targetButtons[i].gameObject.SetActive(false);
            }
            var btnBack = CreateCommandButton(cmdInner, "BACK");
            btnBack.gameObject.SetActive(false);

            // Spacer to push mode toggle to bottom
            var spacerGO = new GameObject("Spacer");
            spacerGO.transform.SetParent(cmdInner, false);
            var spacerLE = spacerGO.AddComponent<LayoutElement>();
            spacerLE.flexibleHeight = 1;

            // Mode toggle button (Wait / Active)
            var btnMode = CreateCommandButton(cmdInner, "MODE: WAIT");

            // ── Message Log (center) ──
            var msgInner = CreateBorderedWindow(root, "MessagePanel",
                new Vector2(0.16f, 0.03f), new Vector2(0.68f, 0.73f));

            var msgScroll = CreateDemoMessageArea(msgInner, "Simple JRPG-ATB Battle Turn Demo Scene");

            // ── Party Panel (right) ──
            var partyInner = CreateBorderedWindow(root, "PartyPanel",
                new Vector2(0.69f, 0.03f), new Vector2(0.97f, 0.73f));

            var partyVLG = partyInner.gameObject.AddComponent<VerticalLayoutGroup>();
            partyVLG.padding = new RectOffset(10, 10, 10, 10);
            partyVLG.spacing = 6;
            partyVLG.childControlWidth = true;
            partyVLG.childControlHeight = true;
            partyVLG.childForceExpandWidth = false;
            partyVLG.childForceExpandHeight = false;

            var partyPortraits = new Image[4];
            var partyNameTexts = new TextMeshProUGUI[4];
            var partyHPBars = new Slider[4];
            var partyMPBars = new Slider[4];
            var partyHPTexts = new TextMeshProUGUI[4];
            var partyMPTexts = new TextMeshProUGUI[4];
            var partySelectMarks = new GameObject[4];
            var partyATBBars = new Slider[4];

            for (int i = 0; i < 4; i++)
            {
                var card = CreateCharacterCard(partyInner, $"Party_{i}", true);
                partyPortraits[i] = card.portrait;
                partyNameTexts[i] = card.nameText;
                partyHPBars[i] = card.hpBar;
                partyMPBars[i] = card.mpBar;
                partyHPTexts[i] = card.hpText;
                partyMPTexts[i] = card.mpText;
                partySelectMarks[i] = card.selectMark;

                // ATB label + bar in stats column
                var statsT = card.nameText.transform.parent;
                var partyATBLabel = CreateText(statsT, "ATBLabel", "ATB", 14);
                var partyAtbLabelLE = partyATBLabel.gameObject.AddComponent<LayoutElement>();
                partyAtbLabelLE.preferredHeight = 16;
                partyAtbLabelLE.preferredWidth = 200;
                partyATBBars[i] = CreateBar(statsT, "ATBBar", BarATB);
                var partyAtbBarLE = partyATBBars[i].gameObject.AddComponent<LayoutElement>();
                partyAtbBarLE.preferredHeight = 14;
                partyAtbBarLE.preferredWidth = 200;
                partyATBBars[i].value = 0;

                // Bump heights to fit ATB label + bar
                statsT.GetComponent<LayoutElement>().preferredHeight = 120;
                card.portrait.transform.parent.GetComponent<LayoutElement>().preferredHeight = 150;
            }

            // ═══════════════════════════════════════
            // WIRE UP
            // ═══════════════════════════════════════

            var demo = canvasGO.AddComponent<ATBTurnDemo>();

            demo.messageContent = msgScroll.content;
            demo.messageScrollRect = msgScroll;

            demo.enemyPortraits = enemyPortraits;
            demo.enemyNameTexts = enemyNameTexts;
            demo.enemyHPBars = enemyHPBars;
            demo.enemyHPTexts = enemyHPTexts;
            demo.enemyATBBars = enemyATBBars;

            demo.partyPortraits = partyPortraits;
            demo.partyNameTexts = partyNameTexts;
            demo.partyHPBars = partyHPBars;
            demo.partyMPBars = partyMPBars;
            demo.partyHPTexts = partyHPTexts;
            demo.partyMPTexts = partyMPTexts;
            demo.partyATBBars = partyATBBars;

            demo.btnFight = btnFight;
            demo.btnHeal = btnHeal;
            demo.btnDefend = btnDefend;
            demo.btnRevive = btnRevive;
            demo.btnFlee = btnFlee;
            demo.commandPanel = cmdInner.gameObject;

            demo.targetButtons = targetButtons;
            demo.btnBack = btnBack;

            demo.partySelectMarks = partySelectMarks;
            demo.enemySelectMarks = enemySelectMarks;

            demo.btnMode = btnMode;

            // ── Save ──
            string dir = "Assets/SimpleJRPG/Scenes";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            EditorSceneManager.SaveScene(scene, $"{dir}/ATBTurnDemo.unity");
            AssetDatabase.Refresh();
            Debug.Log("SimpleJRPG: Generated ATB Turn Demo");
        }

        // [MenuItem("Window/Living Failure/Simple JRPG/Generators/Generate Timeline Turn Demo")]
        public static void GenerateTimelineTurnDemo()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = BgBlack;
            }

            // Canvas
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // EventSystem
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
                var inputSystemType = System.Type.GetType(
                    "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputSystemType != null)
                    eventSystem.AddComponent(inputSystemType);
                else
                    eventSystem.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
            }

            var root = canvasGO.transform;

            // ── Enemy Panel (top) ──
            var enemyInner = CreateBorderedWindow(root, "EnemyPanel",
                new Vector2(0.03f, 0.75f), new Vector2(0.97f, 0.97f));

            var enemyHLG = enemyInner.gameObject.AddComponent<HorizontalLayoutGroup>();
            enemyHLG.padding = new RectOffset(20, 20, 10, 10);
            enemyHLG.spacing = 40;
            enemyHLG.childControlWidth = true;
            enemyHLG.childControlHeight = true;
            enemyHLG.childForceExpandWidth = false;
            enemyHLG.childForceExpandHeight = false;
            enemyHLG.childAlignment = TextAnchor.MiddleCenter;

            var enemyPortraits = new Image[3];
            var enemyNameTexts = new TextMeshProUGUI[3];
            var enemyHPBars = new Slider[3];
            var enemyHPTexts = new TextMeshProUGUI[3];
            var enemySelectMarks = new GameObject[3];

            for (int i = 0; i < 3; i++)
            {
                var card = CreateCharacterCard(enemyInner, $"Enemy_{i}", false);
                enemyPortraits[i] = card.portrait;
                enemyNameTexts[i] = card.nameText;
                enemyHPBars[i] = card.hpBar;
                enemyHPTexts[i] = card.hpText;
                enemySelectMarks[i] = card.selectMark;
            }

            // ── Command Panel (left) ──
            var cmdInner = CreateBorderedWindow(root, "CommandPanel",
                new Vector2(0.03f, 0.03f), new Vector2(0.15f, 0.73f));

            var cmdVLG = cmdInner.gameObject.AddComponent<VerticalLayoutGroup>();
            cmdVLG.padding = new RectOffset(8, 8, 15, 15);
            cmdVLG.spacing = 8;
            cmdVLG.childControlWidth = true;
            cmdVLG.childControlHeight = true;
            cmdVLG.childForceExpandWidth = true;
            cmdVLG.childForceExpandHeight = false;
            cmdVLG.childAlignment = TextAnchor.UpperCenter;

            var btnFight = CreateCommandButton(cmdInner, "FIGHT [100]");
            var btnHeal = CreateCommandButton(cmdInner, "HEAL [80]");
            var btnRevive = CreateCommandButton(cmdInner, "REVIVE [150]");
            var btnDefend = CreateCommandButton(cmdInner, "DEFEND [50]");
            var btnFlee = CreateCommandButton(cmdInner, "FLEE");

            // Target buttons (same panel, hidden until Fight is pressed)
            var targetButtons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                targetButtons[i] = CreateCommandButton(cmdInner, "---");
                targetButtons[i].gameObject.SetActive(false);
            }
            var btnBack = CreateCommandButton(cmdInner, "BACK");
            btnBack.gameObject.SetActive(false);

            // ── Message Log (center — narrower to fit timeline) ──
            var msgInner = CreateBorderedWindow(root, "MessagePanel",
                new Vector2(0.16f, 0.03f), new Vector2(0.52f, 0.73f));

            var msgScroll = CreateDemoMessageArea(msgInner, "Simple JRPG-Timeline Battle Turn Demo Scene");

            // ── Timeline Panel (between message and party) ──
            var timelineInner = CreateBorderedWindow(root, "TimelinePanel",
                new Vector2(0.53f, 0.03f), new Vector2(0.68f, 0.73f));

            var timelineVLG = timelineInner.gameObject.AddComponent<VerticalLayoutGroup>();
            timelineVLG.padding = new RectOffset(10, 10, 8, 8);
            timelineVLG.spacing = 2;
            timelineVLG.childControlWidth = true;
            timelineVLG.childControlHeight = true;
            timelineVLG.childForceExpandWidth = false;
            timelineVLG.childForceExpandHeight = false;

            var timelineTitle = CreateText(timelineInner, "Title", "TIMELINE", 20);
            timelineTitle.color = TextGold;
            timelineTitle.fontStyle = FontStyles.Bold;
            var titleLE = timelineTitle.gameObject.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 30;

            var timelineSlots = new TextMeshProUGUI[10];
            for (int i = 0; i < 10; i++)
            {
                timelineSlots[i] = CreateText(timelineInner, $"Slot_{i}", "", 18);
                var slotLE = timelineSlots[i].gameObject.AddComponent<LayoutElement>();
                slotLE.preferredHeight = 26;
            }

            // ── Party Panel (right) ──
            var partyInner = CreateBorderedWindow(root, "PartyPanel",
                new Vector2(0.69f, 0.03f), new Vector2(0.97f, 0.73f));

            var partyVLG = partyInner.gameObject.AddComponent<VerticalLayoutGroup>();
            partyVLG.padding = new RectOffset(10, 10, 10, 10);
            partyVLG.spacing = 6;
            partyVLG.childControlWidth = true;
            partyVLG.childControlHeight = true;
            partyVLG.childForceExpandWidth = false;
            partyVLG.childForceExpandHeight = false;

            var partyPortraits = new Image[4];
            var partyNameTexts = new TextMeshProUGUI[4];
            var partyHPBars = new Slider[4];
            var partyMPBars = new Slider[4];
            var partyHPTexts = new TextMeshProUGUI[4];
            var partyMPTexts = new TextMeshProUGUI[4];
            var partySelectMarks = new GameObject[4];

            for (int i = 0; i < 4; i++)
            {
                var card = CreateCharacterCard(partyInner, $"Party_{i}", true);
                partyPortraits[i] = card.portrait;
                partyNameTexts[i] = card.nameText;
                partyHPBars[i] = card.hpBar;
                partyMPBars[i] = card.mpBar;
                partyHPTexts[i] = card.hpText;
                partyMPTexts[i] = card.mpText;
                partySelectMarks[i] = card.selectMark;
            }

            // ═══════════════════════════════════════
            // WIRE UP
            // ═══════════════════════════════════════

            var demo = canvasGO.AddComponent<TimelineTurnDemo>();

            demo.messageContent = msgScroll.content;
            demo.messageScrollRect = msgScroll;

            demo.enemyPortraits = enemyPortraits;
            demo.enemyNameTexts = enemyNameTexts;
            demo.enemyHPBars = enemyHPBars;
            demo.enemyHPTexts = enemyHPTexts;

            demo.partyPortraits = partyPortraits;
            demo.partyNameTexts = partyNameTexts;
            demo.partyHPBars = partyHPBars;
            demo.partyMPBars = partyMPBars;
            demo.partyHPTexts = partyHPTexts;
            demo.partyMPTexts = partyMPTexts;

            demo.btnFight = btnFight;
            demo.btnHeal = btnHeal;
            demo.btnDefend = btnDefend;
            demo.btnRevive = btnRevive;
            demo.btnFlee = btnFlee;
            demo.commandPanel = cmdInner.gameObject;

            demo.targetButtons = targetButtons;
            demo.btnBack = btnBack;

            demo.partySelectMarks = partySelectMarks;
            demo.enemySelectMarks = enemySelectMarks;

            demo.timelineSlots = timelineSlots;

            // ── Save ──
            string dir = "Assets/SimpleJRPG/Scenes";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            EditorSceneManager.SaveScene(scene, $"{dir}/TimelineTurnDemo.unity");
            AssetDatabase.Refresh();
            Debug.Log("SimpleJRPG: Generated Timeline Turn Demo (FFX style)");
        }

        // [MenuItem("Window/Living Failure/Simple JRPG/Generators/Generate Press Turn Demo")]
        public static void GeneratePressTurnDemo()
        {
            // Canvas
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // EventSystem
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
                var inputSystemType = System.Type.GetType(
                    "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputSystemType != null)
                    eventSystem.AddComponent(inputSystemType);
                else
                    eventSystem.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
            }

            var root = canvasGO.transform;

            // ═══════════════════════════════════════
            // SMT-STYLE PRESS TURN LAYOUT
            // ═══════════════════════════════════════
            //
            //  ┌──────────────────────────────────────────────────┐
            //  │  [Wraith]       [Pixie]       [Shade]    ENEMY  │
            //  ├──────────────────────────────────────────────────┤
            //  │  PLAYER PHASE                    ● ● ● ●  PHASE │
            //  ├────────┬───────────────────────────┬─────────────┤
            //  │ ACTOR  │    MESSAGE LOG            │ [Nahobino]  │
            //  │ SELECT │                           │  HP ████    │
            //  │  or    │                           │ [Invoker]   │
            //  │ CMDS   │                           │  HP ████    │
            //  │  or    │                           │ [Healer]    │
            //  │ TARGET │                           │  HP ████    │
            //  │        │                           │ [Sentinel]  │
            //  └────────┴───────────────────────────┴─────────────┘

            // ── Enemy Panel (top) ──
            var enemyInner = CreateBorderedWindow(root, "EnemyPanel",
                new Vector2(0.03f, 0.78f), new Vector2(0.97f, 0.97f));

            var enemyHLG = enemyInner.gameObject.AddComponent<HorizontalLayoutGroup>();
            enemyHLG.padding = new RectOffset(20, 20, 10, 10);
            enemyHLG.spacing = 40;
            enemyHLG.childControlWidth = true;
            enemyHLG.childControlHeight = true;
            enemyHLG.childForceExpandWidth = false;
            enemyHLG.childForceExpandHeight = false;
            enemyHLG.childAlignment = TextAnchor.MiddleCenter;

            var enemyPortraits = new Image[3];
            var enemyNameTexts = new TextMeshProUGUI[3];
            var enemyHPBars = new Slider[3];
            var enemyHPTexts = new TextMeshProUGUI[3];
            var enemySelectMarks = new GameObject[3];

            for (int i = 0; i < 3; i++)
            {
                var card = CreateCharacterCard(enemyInner, $"Enemy_{i}", false);
                enemyPortraits[i] = card.portrait;
                enemyNameTexts[i] = card.nameText;
                enemyHPBars[i] = card.hpBar;
                enemyHPTexts[i] = card.hpText;
                enemySelectMarks[i] = card.selectMark;
            }

            // ── Phase/Icons Panel ──
            var phaseInner = CreateBorderedWindow(root, "PhasePanel",
                new Vector2(0.03f, 0.71f), new Vector2(0.97f, 0.77f));

            var phaseHLG = phaseInner.gameObject.AddComponent<HorizontalLayoutGroup>();
            phaseHLG.padding = new RectOffset(20, 20, 5, 5);
            phaseHLG.spacing = 10;
            phaseHLG.childControlWidth = true;
            phaseHLG.childControlHeight = true;
            phaseHLG.childForceExpandWidth = false;
            phaseHLG.childForceExpandHeight = false;
            phaseHLG.childAlignment = TextAnchor.MiddleLeft;

            var phaseLabel = CreateText(phaseInner, "PhaseLabel", "PLAYER PHASE", 28);
            phaseLabel.fontStyle = FontStyles.Bold;
            phaseLabel.color = TextGold;
            var phaseLabelLE = phaseLabel.gameObject.AddComponent<LayoutElement>();
            phaseLabelLE.preferredWidth = 300;

            // Spacer
            var phaseSpacer = new GameObject("Spacer");
            phaseSpacer.transform.SetParent(phaseInner, false);
            var phaseSpacerLE = phaseSpacer.AddComponent<LayoutElement>();
            phaseSpacerLE.flexibleWidth = 1;

            // Press turn icons (4 slots)
            var pressIcons = new TextMeshProUGUI[4];
            for (int i = 0; i < 4; i++)
            {
                pressIcons[i] = CreateText(phaseInner, $"PressIcon_{i}", "[+]", 32);
                pressIcons[i].alignment = TextAlignmentOptions.Center;
                var iconLE = pressIcons[i].gameObject.AddComponent<LayoutElement>();
                iconLE.preferredWidth = 40;
            }

            // ── Command Panel (left) ──
            var cmdInner = CreateBorderedWindow(root, "CommandPanel",
                new Vector2(0.03f, 0.03f), new Vector2(0.15f, 0.70f));

            var cmdVLG = cmdInner.gameObject.AddComponent<VerticalLayoutGroup>();
            cmdVLG.padding = new RectOffset(8, 8, 15, 15);
            cmdVLG.spacing = 8;
            cmdVLG.childControlWidth = true;
            cmdVLG.childControlHeight = true;
            cmdVLG.childForceExpandWidth = true;
            cmdVLG.childForceExpandHeight = false;
            cmdVLG.childAlignment = TextAnchor.UpperCenter;

            // Actor selection buttons (4, hidden initially by demo)
            var actorButtons = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                actorButtons[i] = CreateCommandButton(cmdInner, "---");
            }

            // Command buttons (hidden initially by demo)
            var btnFight = CreateCommandButton(cmdInner, "FIGHT");
            btnFight.gameObject.SetActive(false);
            var btnMagic = CreateCommandButton(cmdInner, "MAGIC");
            btnMagic.gameObject.SetActive(false);
            var btnHeal = CreateCommandButton(cmdInner, "HEAL");
            btnHeal.gameObject.SetActive(false);
            var btnPass = CreateCommandButton(cmdInner, "PASS");
            btnPass.gameObject.SetActive(false);
            var btnFlee = CreateCommandButton(cmdInner, "FLEE");
            btnFlee.gameObject.SetActive(false);

            // Target buttons (3, hidden initially)
            var targetButtons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                targetButtons[i] = CreateCommandButton(cmdInner, "---");
                targetButtons[i].gameObject.SetActive(false);
            }
            var btnBack = CreateCommandButton(cmdInner, "BACK");
            btnBack.gameObject.SetActive(false);

            // ── Message Log (center) ──
            var msgInner = CreateBorderedWindow(root, "MessagePanel",
                new Vector2(0.16f, 0.03f), new Vector2(0.68f, 0.70f));

            var msgScroll = CreateDemoMessageArea(msgInner, "Simple JRPG-Press Turn Demo Scene");

            // ── Party Panel (right) ──
            var partyInner = CreateBorderedWindow(root, "PartyPanel",
                new Vector2(0.69f, 0.03f), new Vector2(0.97f, 0.70f));

            var partyVLG = partyInner.gameObject.AddComponent<VerticalLayoutGroup>();
            partyVLG.padding = new RectOffset(10, 10, 10, 10);
            partyVLG.spacing = 6;
            partyVLG.childControlWidth = true;
            partyVLG.childControlHeight = true;
            partyVLG.childForceExpandWidth = false;
            partyVLG.childForceExpandHeight = false;

            var partyPortraits = new Image[4];
            var partyNameTexts = new TextMeshProUGUI[4];
            var partyHPBars = new Slider[4];
            var partyMPBars = new Slider[4];
            var partyHPTexts = new TextMeshProUGUI[4];
            var partyMPTexts = new TextMeshProUGUI[4];
            var partySelectMarks = new GameObject[4];

            for (int i = 0; i < 4; i++)
            {
                var card = CreateCharacterCard(partyInner, $"Party_{i}", true);
                partyPortraits[i] = card.portrait;
                partyNameTexts[i] = card.nameText;
                partyHPBars[i] = card.hpBar;
                partyMPBars[i] = card.mpBar;
                partyHPTexts[i] = card.hpText;
                partyMPTexts[i] = card.mpText;
                partySelectMarks[i] = card.selectMark;
            }

            // ═══════════════════════════════════════
            // WIRE UP
            // ═══════════════════════════════════════

            var demo = canvasGO.AddComponent<PressTurnDemo>();

            demo.messageContent = msgScroll.content;
            demo.messageScrollRect = msgScroll;

            demo.enemyPortraits = enemyPortraits;
            demo.enemyNameTexts = enemyNameTexts;
            demo.enemyHPBars = enemyHPBars;
            demo.enemyHPTexts = enemyHPTexts;

            demo.partyPortraits = partyPortraits;
            demo.partyNameTexts = partyNameTexts;
            demo.partyHPBars = partyHPBars;
            demo.partyMPBars = partyMPBars;
            demo.partyHPTexts = partyHPTexts;
            demo.partyMPTexts = partyMPTexts;

            demo.btnFight = btnFight;
            demo.btnMagic = btnMagic;
            demo.btnHeal = btnHeal;
            demo.btnPass = btnPass;
            demo.btnFlee = btnFlee;
            demo.commandPanel = cmdInner.gameObject;

            demo.actorButtons = actorButtons;
            demo.targetButtons = targetButtons;
            demo.btnBack = btnBack;

            demo.partySelectMarks = partySelectMarks;
            demo.enemySelectMarks = enemySelectMarks;

            demo.phaseLabel = phaseLabel;
            demo.pressIcons = pressIcons;

            Debug.Log("SimpleJRPG: Generated Press Turn Demo (SMT style)");
        }

        // [MenuItem("Window/Living Failure/Simple JRPG/Generators/Generate Action Point Demo")]
        public static void GenerateActionPointDemo()
        {
            // Canvas
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // EventSystem
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
                var inputSystemType = System.Type.GetType(
                    "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputSystemType != null)
                    eventSystem.AddComponent(inputSystemType);
                else
                    eventSystem.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
            }

            var root = canvasGO.transform;

            // ═══════════════════════════════════════
            // ACTION POINT LAYOUT
            // ═══════════════════════════════════════
            //
            //  ┌──────────────────────────────────────────────────┐
            //  │  [Troll]       [Goblin]      [Hound]     ENEMY  │
            //  ├────────┬───────────────────────────┬─────────────┤
            //  │ SPEND  │                           │ [Hero]      │
            //  │ SAVE   │    MESSAGE LOG            │  HP ████    │
            //  │ FIGHT  │                           │  MP ██      │
            //  │ MAGIC  │                           │  AP: 1      │
            //  │ HEAL   │                           │ [Mage]      │
            //  │ FLEE   │                           │  ...        │
            //  └────────┴───────────────────────────┴─────────────┘

            // ── Enemy Panel (top) ──
            var enemyInner = CreateBorderedWindow(root, "EnemyPanel",
                new Vector2(0.03f, 0.75f), new Vector2(0.97f, 0.97f));

            var enemyHLG = enemyInner.gameObject.AddComponent<HorizontalLayoutGroup>();
            enemyHLG.padding = new RectOffset(20, 20, 10, 10);
            enemyHLG.spacing = 40;
            enemyHLG.childControlWidth = true;
            enemyHLG.childControlHeight = true;
            enemyHLG.childForceExpandWidth = false;
            enemyHLG.childForceExpandHeight = false;
            enemyHLG.childAlignment = TextAnchor.MiddleCenter;

            var enemyPortraits = new Image[3];
            var enemyNameTexts = new TextMeshProUGUI[3];
            var enemyHPBars = new Slider[3];
            var enemyHPTexts = new TextMeshProUGUI[3];
            var enemySelectMarks = new GameObject[3];
            var enemyAPTexts = new TextMeshProUGUI[3];

            for (int i = 0; i < 3; i++)
            {
                var card = CreateCharacterCard(enemyInner, $"Enemy_{i}", false);
                enemyPortraits[i] = card.portrait;
                enemyNameTexts[i] = card.nameText;
                enemyHPBars[i] = card.hpBar;
                enemyHPTexts[i] = card.hpText;
                enemySelectMarks[i] = card.selectMark;

                // AP text in stats column
                var statsT = card.nameText.transform.parent;
                enemyAPTexts[i] = CreateText(statsT, "APText", "AP: 0", 16);
                var apLE = enemyAPTexts[i].gameObject.AddComponent<LayoutElement>();
                apLE.preferredHeight = 18;
                apLE.preferredWidth = 200;

                statsT.GetComponent<LayoutElement>().preferredHeight = 100;
                card.portrait.transform.parent.GetComponent<LayoutElement>().preferredHeight = 120;
            }

            // ── Command Panel (left) ──
            var cmdInner = CreateBorderedWindow(root, "CommandPanel",
                new Vector2(0.03f, 0.03f), new Vector2(0.15f, 0.73f));

            var cmdVLG = cmdInner.gameObject.AddComponent<VerticalLayoutGroup>();
            cmdVLG.padding = new RectOffset(8, 8, 15, 15);
            cmdVLG.spacing = 8;
            cmdVLG.childControlWidth = true;
            cmdVLG.childControlHeight = true;
            cmdVLG.childForceExpandWidth = true;
            cmdVLG.childForceExpandHeight = false;
            cmdVLG.childAlignment = TextAnchor.UpperCenter;

            var btnSpend = CreateCommandButton(cmdInner, "SPEND");
            var btnSave = CreateCommandButton(cmdInner, "SAVE");
            var btnFight = CreateCommandButton(cmdInner, "FIGHT");
            var btnMagic = CreateCommandButton(cmdInner, "MAGIC");
            var btnHeal = CreateCommandButton(cmdInner, "HEAL");
            var btnFlee = CreateCommandButton(cmdInner, "FLEE");

            // Target buttons (same panel, hidden until Fight/Magic is pressed)
            var targetButtons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                targetButtons[i] = CreateCommandButton(cmdInner, "---");
                targetButtons[i].gameObject.SetActive(false);
            }
            var btnBack = CreateCommandButton(cmdInner, "BACK");
            btnBack.gameObject.SetActive(false);

            // ── Message Log (center) ──
            var msgInner = CreateBorderedWindow(root, "MessagePanel",
                new Vector2(0.16f, 0.03f), new Vector2(0.68f, 0.73f));

            var msgScroll = CreateDemoMessageArea(msgInner, "Simple JRPG-Action Point Demo Scene");

            // ── Party Panel (right) ──
            var partyInner = CreateBorderedWindow(root, "PartyPanel",
                new Vector2(0.69f, 0.03f), new Vector2(0.97f, 0.73f));

            var partyVLG = partyInner.gameObject.AddComponent<VerticalLayoutGroup>();
            partyVLG.padding = new RectOffset(10, 10, 10, 10);
            partyVLG.spacing = 6;
            partyVLG.childControlWidth = true;
            partyVLG.childControlHeight = true;
            partyVLG.childForceExpandWidth = false;
            partyVLG.childForceExpandHeight = false;

            var partyPortraits = new Image[4];
            var partyNameTexts = new TextMeshProUGUI[4];
            var partyHPBars = new Slider[4];
            var partyMPBars = new Slider[4];
            var partyHPTexts = new TextMeshProUGUI[4];
            var partyMPTexts = new TextMeshProUGUI[4];
            var partySelectMarks = new GameObject[4];
            var partyAPTexts = new TextMeshProUGUI[4];

            for (int i = 0; i < 4; i++)
            {
                var card = CreateCharacterCard(partyInner, $"Party_{i}", true);
                partyPortraits[i] = card.portrait;
                partyNameTexts[i] = card.nameText;
                partyHPBars[i] = card.hpBar;
                partyMPBars[i] = card.mpBar;
                partyHPTexts[i] = card.hpText;
                partyMPTexts[i] = card.mpText;
                partySelectMarks[i] = card.selectMark;

                // AP text in stats column
                var statsT = card.nameText.transform.parent;
                partyAPTexts[i] = CreateText(statsT, "APText", "AP: 0", 16);
                var apLE = partyAPTexts[i].gameObject.AddComponent<LayoutElement>();
                apLE.preferredHeight = 18;
                apLE.preferredWidth = 200;

                statsT.GetComponent<LayoutElement>().preferredHeight = 120;
                card.portrait.transform.parent.GetComponent<LayoutElement>().preferredHeight = 150;
            }

            // ═══════════════════════════════════════
            // WIRE UP
            // ═══════════════════════════════════════

            var demo = canvasGO.AddComponent<ActionPointDemo>();

            demo.messageContent = msgScroll.content;
            demo.messageScrollRect = msgScroll;

            demo.enemyPortraits = enemyPortraits;
            demo.enemyNameTexts = enemyNameTexts;
            demo.enemyHPBars = enemyHPBars;
            demo.enemyHPTexts = enemyHPTexts;
            demo.enemyAPTexts = enemyAPTexts;

            demo.partyPortraits = partyPortraits;
            demo.partyNameTexts = partyNameTexts;
            demo.partyHPBars = partyHPBars;
            demo.partyMPBars = partyMPBars;
            demo.partyHPTexts = partyHPTexts;
            demo.partyMPTexts = partyMPTexts;
            demo.partyAPTexts = partyAPTexts;

            demo.btnSpend = btnSpend;
            demo.btnSave = btnSave;
            demo.btnFight = btnFight;
            demo.btnMagic = btnMagic;
            demo.btnHeal = btnHeal;
            demo.btnFlee = btnFlee;
            demo.commandPanel = cmdInner.gameObject;

            demo.targetButtons = targetButtons;
            demo.btnBack = btnBack;

            demo.partySelectMarks = partySelectMarks;
            demo.enemySelectMarks = enemySelectMarks;

            Debug.Log("SimpleJRPG: Generated Action Point Demo");
        }

        // ── Documentation Menu Items ──

        private const string DocsRoot = "Assets/SimpleJRPG/Documentation/";

        [MenuItem("Window/Living Failure/Simple JRPG/Documentation/Open All (index)")]
        public static void OpenDocsIndex() => OpenDoc("index.html");

        [MenuItem("Window/Living Failure/Simple JRPG/Documentation/Installation")]
        public static void OpenDocsInstallation() => OpenDoc("installation.html");

        [MenuItem("Window/Living Failure/Simple JRPG/Documentation/Quick Start")]
        public static void OpenDocsQuickStart() => OpenDoc("quick-start.html");

        [MenuItem("Window/Living Failure/Simple JRPG/Documentation/Battle Core")]
        public static void OpenDocsBattleCore() => OpenDoc("battle-core.html");

        [MenuItem("Window/Living Failure/Simple JRPG/Documentation/Before-Events")]
        public static void OpenDocsBeforeEvents() => OpenDoc("before-events.html");

        [MenuItem("Window/Living Failure/Simple JRPG/Documentation/Helpers")]
        public static void OpenDocsHelpers() => OpenDoc("helpers.html");

        [MenuItem("Window/Living Failure/Simple JRPG/Documentation/Turn Systems")]
        public static void OpenDocsTurnSystems() => OpenDoc("turn-systems.html");

        [MenuItem("Window/Living Failure/Simple JRPG/Documentation/Cookbook")]
        public static void OpenDocsCookbook() => OpenDoc("cookbook.html");

        [MenuItem("Window/Living Failure/Simple JRPG/Documentation/API Reference")]
        public static void OpenDocsAPIReference() => OpenDoc("api-reference.html");

        [MenuItem("Window/Living Failure/Simple JRPG/Documentation/Demos")]
        public static void OpenDocsDemos() => OpenDoc("demos.html");

        [MenuItem("Window/Living Failure/Simple JRPG/Documentation/About")]
        public static void OpenDocsAbout() => OpenDoc("about.html");

        [MenuItem("Window/Living Failure/Simple JRPG/Documentation/Single Page (All)")]
        public static void OpenDocsSinglePage() => OpenDoc("single-page.html");

        private static void OpenDoc(string filename)
        {
            string fullPath = Path.GetFullPath(Path.Combine(DocsRoot, filename));
            if (File.Exists(fullPath))
                Application.OpenURL("file:///" + fullPath.Replace('\\', '/'));
            else
                Debug.LogWarning($"SimpleJRPG: Documentation file not found: {fullPath}");
        }

        // ═══════════════════════════════════════════
        // UI HELPERS
        // ═══════════════════════════════════════════

        private static ScrollRect CreateDemoMessageArea(RectTransform msgInner, string titleText)
        {
            // Inner bg → pink (title and button sit on pink), scroll area is dark
            msgInner.GetComponent<Image>().color = BorderPink;

            // Demo title — full width at top, 150px tall
            var title = CreateText(msgInner, "DemoScene", titleText, 50);
            title.alignment = TextAlignmentOptions.Center;
            var titleRT = title.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1);
            titleRT.anchorMax = new Vector2(1, 1);
            titleRT.pivot = new Vector2(0.5f, 1);
            titleRT.anchoredPosition = Vector2.zero;
            titleRT.sizeDelta = new Vector2(0, 150);

            // Message scroll — horizontal stretch, 440px tall, centered vertically
            var msgScroll = CreateScrollView(msgInner);
            var scrollRT = (RectTransform)msgScroll.transform;
            scrollRT.anchorMin = new Vector2(0, 0.5f);
            scrollRT.anchorMax = new Vector2(1, 0.5f);
            scrollRT.pivot = new Vector2(0.5f, 0.5f);
            scrollRT.anchoredPosition = Vector2.zero;
            scrollRT.sizeDelta = new Vector2(-20, 440);

            // Dark bg on the scroll area itself
            msgScroll.gameObject.AddComponent<Image>().color = WindowBg;

            // White scrollbar handle
            msgScroll.verticalScrollbar.handleRect.GetComponent<Image>().color = Color.white;

            // Back to main menu button — bottom center
            var btnGO = new GameObject("BackToMainMenu");
            btnGO.transform.SetParent(msgInner, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.5f, 0);
            btnRT.anchorMax = new Vector2(0.5f, 0);
            btnRT.pivot = new Vector2(0.5f, 0);
            btnRT.anchoredPosition = new Vector2(0, 8);
            btnRT.sizeDelta = new Vector2(256, 64);

            btnGO.AddComponent<Image>().color = Color.black;
            btnGO.AddComponent<Button>();

            var labelGO = new GameObject("BackToMainMenu");
            labelGO.transform.SetParent(btnGO.transform, false);
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = "Main Menu";
            labelTMP.fontSize = 24;
            labelTMP.color = Color.white;
            labelTMP.alignment = TextAlignmentOptions.Center;

            return msgScroll;
        }

        private struct CharacterCard
        {
            public Image portrait;
            public TextMeshProUGUI nameText;
            public Slider hpBar;
            public Slider mpBar;
            public TextMeshProUGUI hpText;
            public TextMeshProUGUI mpText;
            public GameObject selectMark;
        }

        private static CharacterCard CreateCharacterCard(RectTransform parent, string name, bool showMP)
        {
            var card = new CharacterCard();

            // Card container — horizontal: [portrait] [stats VLG]
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.padding = new RectOffset(5, 5, 4, 4);
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            int cardHeight = showMP ? 110 : 100;
            var cardLE = go.AddComponent<LayoutElement>();
            cardLE.preferredHeight = cardHeight;

            // SelectMark — first in HLG, thin pink bar
            var markGO = new GameObject("SelectMark");
            markGO.transform.SetParent(go.transform, false);
            markGO.AddComponent<Image>().color = BorderPink;
            var markLE = markGO.AddComponent<LayoutElement>();
            markLE.preferredWidth = 6;
            markLE.preferredHeight = 80;
            markGO.SetActive(false);
            card.selectMark = markGO;

            // Portrait (80x80)
            var portraitGO = new GameObject("Portrait");
            portraitGO.transform.SetParent(go.transform, false);
            card.portrait = portraitGO.AddComponent<Image>();
            card.portrait.color = PortraitBg;
            var portraitLE = portraitGO.AddComponent<LayoutElement>();
            portraitLE.preferredWidth = 80;
            portraitLE.preferredHeight = 80;

            // Stats column
            var statsGO = new GameObject("Stats");
            statsGO.transform.SetParent(go.transform, false);

            var statsVLG = statsGO.AddComponent<VerticalLayoutGroup>();
            statsVLG.spacing = 2;
            statsVLG.childControlWidth = true;
            statsVLG.childControlHeight = true;
            statsVLG.childForceExpandWidth = false;
            statsVLG.childForceExpandHeight = false;

            var statsLE = statsGO.AddComponent<LayoutElement>();
            statsLE.preferredWidth = 200;
            statsLE.preferredHeight = 80;

            // Name
            card.nameText = CreateText(statsGO.transform, "Name", "---", 22);
            card.nameText.color = TextGold;
            var nameLE = card.nameText.gameObject.AddComponent<LayoutElement>();
            nameLE.preferredHeight = 26;

            // HP bar row: label + slider
            card.hpText = CreateText(statsGO.transform, "HPLabel", "HP ---", 16);
            var hpTextLE = card.hpText.gameObject.AddComponent<LayoutElement>();
            hpTextLE.preferredHeight = 18;

            card.hpBar = CreateBar(statsGO.transform, "HPBar", BarGreen);
            var hpBarLE = card.hpBar.gameObject.AddComponent<LayoutElement>();
            hpBarLE.preferredHeight = 14;

            // MP bar (party only)
            if (showMP)
            {
                card.mpText = CreateText(statsGO.transform, "MPLabel", "MP ---", 16);
                var mpTextLE = card.mpText.gameObject.AddComponent<LayoutElement>();
                mpTextLE.preferredHeight = 18;

                card.mpBar = CreateBar(statsGO.transform, "MPBar", BarBlue);
                var mpBarLE = card.mpBar.gameObject.AddComponent<LayoutElement>();
                mpBarLE.preferredHeight = 14;
            }

            return card;
        }

        private static Slider CreateBar(Transform parent, string name, Color fillColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var slider = go.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 1;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(go.transform, false);
            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            bgGO.AddComponent<Image>().color = BarBgDark;

            // Fill Area
            var fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(go.transform, false);
            var fillAreaRT = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = Vector2.zero;
            fillAreaRT.anchorMax = Vector2.one;
            fillAreaRT.offsetMin = Vector2.zero;
            fillAreaRT.offsetMax = Vector2.zero;

            // Fill
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            var fillRT = fillGO.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(0, 1);
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            fillGO.AddComponent<Image>().color = fillColor;

            slider.fillRect = fillRT;

            return slider;
        }

        private static RectTransform CreateBorderedWindow(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var outer = new GameObject(name);
            outer.transform.SetParent(parent, false);
            var outerRT = outer.AddComponent<RectTransform>();
            outerRT.anchorMin = anchorMin;
            outerRT.anchorMax = anchorMax;
            outerRT.offsetMin = Vector2.zero;
            outerRT.offsetMax = Vector2.zero;

            outer.AddComponent<Image>().color = BorderPink;

            var inner = new GameObject("Inner");
            inner.transform.SetParent(outer.transform, false);
            var innerRT = inner.AddComponent<RectTransform>();
            innerRT.anchorMin = Vector2.zero;
            innerRT.anchorMax = Vector2.one;
            innerRT.offsetMin = new Vector2(3, 3);
            innerRT.offsetMax = new Vector2(-3, -3);

            inner.AddComponent<Image>().color = WindowBg;

            return innerRT;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string content, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.color = TextWhite;
            tmp.richText = true;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;

            return tmp;
        }

        private static Button CreateCommandButton(RectTransform parent, string label)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = WindowBg;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = BorderPink;
            outline.effectDistance = new Vector2(2, 2);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = WindowBg;
            colors.highlightedColor = BtnHighlight;
            colors.pressedColor = BtnPressed;
            colors.selectedColor = WindowBg;
            btn.colors = colors;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 55;

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            var txt = labelGO.AddComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.fontSize = 22;
            txt.color = TextWhite;
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontStyle = FontStyles.Bold;

            return btn;
        }

        private static ScrollRect CreateScrollView(RectTransform parent)
        {
            var go = new GameObject("MessageScroll");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(10, 8);
            rt.offsetMax = new Vector2(-10, -8);

            var scrollRect = go.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Viewport
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(go.transform, false);
            var vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero;
            vpRT.offsetMax = new Vector2(-14, 0);

            viewport.AddComponent<Image>().color = WindowBg;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            scrollRect.viewport = vpRT;

            // Content
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRT = content.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0, 1);
            contentRT.offsetMin = Vector2.zero;
            contentRT.offsetMax = Vector2.zero;

            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 2;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRT;

            // Scrollbar
            var sbGO = new GameObject("Scrollbar");
            sbGO.transform.SetParent(go.transform, false);
            var sbRT = sbGO.AddComponent<RectTransform>();
            sbRT.anchorMin = new Vector2(1, 0);
            sbRT.anchorMax = Vector2.one;
            sbRT.offsetMin = new Vector2(-12, 0);
            sbRT.offsetMax = Vector2.zero;

            sbGO.AddComponent<Image>().color = BarBgDark;

            var scrollbar = sbGO.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var slidingArea = new GameObject("Sliding Area");
            slidingArea.transform.SetParent(sbGO.transform, false);
            var saRT = slidingArea.AddComponent<RectTransform>();
            saRT.anchorMin = Vector2.zero;
            saRT.anchorMax = Vector2.one;
            saRT.offsetMin = Vector2.zero;
            saRT.offsetMax = Vector2.zero;

            var handle = new GameObject("Handle");
            handle.transform.SetParent(slidingArea.transform, false);
            var hRT = handle.AddComponent<RectTransform>();
            hRT.anchorMin = Vector2.zero;
            hRT.anchorMax = Vector2.one;
            hRT.offsetMin = Vector2.zero;
            hRT.offsetMax = Vector2.zero;
            handle.AddComponent<Image>().color = BorderPink;

            scrollbar.handleRect = hRT;
            scrollbar.targetGraphic = handle.GetComponent<Image>();
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = 0;

            return scrollRect;
        }
    }
}
