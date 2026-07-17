using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SimpleJRPG.Demo.Editor
{
    public class BattleTemplateGenerator : EditorWindow
    {
        private enum TurnSystemType { Classic, ATB, Timeline, PressTurn, ActionPoint }

        private TurnSystemType _turnSystem = TurnSystemType.Classic;
        private TurnSystemType _lastTurnSystem = TurnSystemType.Classic;
        private string _className = "ClassicBattleController";
        private string _namespace = "";
        private string _savePath = "Assets/Scripts/";
        private bool _classNameManuallyEdited;
        private Vector2 _scroll;

        [MenuItem("Window/Living Failure/Simple JRPG/Generate Battle Controller")]
        public static void ShowWindow()
        {
            var window = GetWindow<BattleTemplateGenerator>("Generate Battle Controller");
            window.minSize = new Vector2(460, 420);

            // Size to ~60% of screen, clamped to reasonable bounds
            var screen = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
            float w = Mathf.Clamp(screen.x * 0.6f, 600, 1280);
            float h = Mathf.Clamp(screen.y * 0.6f, 500, 720);
            window.position = new Rect(
                (screen.x - w) * 0.5f,
                (screen.y - h) * 0.5f,
                w, h);
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // Padding
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12);
            EditorGUILayout.BeginVertical();
            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Generates a complete, working MonoBehaviour that runs a battle using SimpleJRPG. " +
                "The generated file compiles and runs immediately with placeholder logic. " +
                "It's yours \u2014 modify freely.\n\n" +
                "After generating, look for TODO comments:\n" +
                "\u2022 Replace MockCombatant with your character class\n" +
                "\u2022 Write your damage/heal formulas\n" +
                "\u2022 Implement your enemy AI targeting\n" +
                "\u2022 Hook up your UI/VFX/audio in event handlers\n" +
                "\u2022 Add your MP costs, cooldowns, and skill logic\n\n" +
                "Prefer to build from scratch? See the API Reference and Quick Start pages in the documentation.",
                MessageType.Info);

            EditorGUILayout.Space(12);

            // ── Settings ──

            // Turn System
            _turnSystem = (TurnSystemType)EditorGUILayout.EnumPopup("Turn System", _turnSystem);
            if (_turnSystem != _lastTurnSystem)
            {
                _lastTurnSystem = _turnSystem;
                if (!_classNameManuallyEdited)
                    _className = GetDefaultClassName(_turnSystem);
            }

            // Class Name
            EditorGUI.BeginChangeCheck();
            _className = EditorGUILayout.TextField("Class Name", _className);
            if (EditorGUI.EndChangeCheck())
                _classNameManuallyEdited = true;

            // Namespace
            _namespace = EditorGUILayout.TextField("Namespace", _namespace);

            // Save Path
            EditorGUILayout.BeginHorizontal();
            _savePath = EditorGUILayout.TextField("Save Path", _savePath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string folder = EditorUtility.OpenFolderPanel("Save Location", "Assets", "");
                if (!string.IsNullOrEmpty(folder))
                {
                    string dataPath = Application.dataPath;
                    if (folder.StartsWith(dataPath))
                        _savePath = "Assets" + folder.Substring(dataPath.Length) + "/";
                    else
                        EditorUtility.DisplayDialog("Invalid Path", "Please select a folder inside your Assets directory.", "OK");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // Validation
            string error = Validate();
            if (!string.IsNullOrEmpty(error))
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
                GUI.enabled = false;
            }

            if (GUILayout.Button("Generate", GUILayout.Height(30)))
                Generate();

            GUI.enabled = true;

            // ── Preview ──

            EditorGUILayout.Space(12);

            string fullPath = Path.Combine(_savePath, _className + ".cs");
            string qualifiedName = string.IsNullOrEmpty(_namespace)
                ? _className
                : _namespace + "." + _className;

            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            var previewStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 11,
                richText = true,
                padding = new RectOffset(8, 8, 6, 6)
            };

            string turnSystemLabel = GetTurnSystemLabel(_turnSystem);

            string preview =
                $"<b>File:</b>  {fullPath}\n" +
                $"<b>Class:</b>  {qualifiedName} : MonoBehaviour\n" +
                $"<b>Turn System:</b>  {turnSystemLabel}\n\n" +
                "<b>What's included:</b>\n" +
                "\u2022 Setup with placeholder party (Hero, Warrior, Mage, Priest) and enemies (Blob, Imp, Mimic)\n" +
                $"\u2022 Battle loop for {turnSystemLabel}\n" +
                "\u2022 Player actions: Attack, Heal, Revive, Defend, Flee, Status, Buff, Multi-Target\n" +
                "\u2022 Enemy AI stub with random targeting\n" +
                "\u2022 All 20 event handlers with Debug.Log placeholders\n" +
                "\u2022 Game logic: CalculateDamage, CalculateHeal, PickTarget, CanAffordSkill, GetFleeChance\n" +
                "\u2022 Battle queries helper + utility methods";

            EditorGUILayout.SelectableLabel(preview, previewStyle,
                GUILayout.MinHeight(180));

            GUILayout.Space(8);
            EditorGUILayout.EndVertical();
            GUILayout.Space(12);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        private static string GetTurnSystemLabel(TurnSystemType type)
        {
            switch (type)
            {
                case TurnSystemType.Classic: return "Classic (speed-sorted round-robin)";
                case TurnSystemType.ATB: return "ATB (gauge-based, real-time)";
                case TurnSystemType.Timeline: return "Timeline / CTB (charge-time)";
                case TurnSystemType.PressTurn: return "Press Turn (team phases, point manipulation)";
                case TurnSystemType.ActionPoint: return "Action Point (save or spend AP)";
                default: return type.ToString();
            }
        }

        private string Validate()
        {
            if (string.IsNullOrWhiteSpace(_className))
                return "Class name cannot be empty.";
            if (!IsValidIdentifier(_className))
                return "Class name is not a valid C# identifier.";
            if (!string.IsNullOrEmpty(_namespace) && !IsValidNamespace(_namespace))
                return "Namespace is not valid. Use dotted identifiers (e.g. MyGame.Battle).";
            if (!_savePath.StartsWith("Assets/") && !_savePath.StartsWith("Assets\\"))
                return "Save path must start with Assets/.";
            return null;
        }

        private static bool IsValidIdentifier(string s)
        {
            return Regex.IsMatch(s, @"^[A-Za-z_][A-Za-z0-9_]*$");
        }

        private static bool IsValidNamespace(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            var parts = s.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                if (!IsValidIdentifier(parts[i]))
                    return false;
            }
            return true;
        }

        private void Generate()
        {
            string filePath = Path.Combine(_savePath, _className + ".cs");

            if (File.Exists(filePath))
            {
                if (!EditorUtility.DisplayDialog("File Exists",
                    $"{filePath} already exists. Overwrite?", "Overwrite", "Cancel"))
                    return;
            }

            string code = GenerateTemplate(_turnSystem, _className, _namespace);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, code);
            AssetDatabase.Refresh();

            Debug.Log($"[SimpleJRPG] Generated {filePath}");
            EditorUtility.DisplayDialog("Done", $"Generated {filePath}", "OK");
        }

        private static string GetDefaultClassName(TurnSystemType type)
        {
            switch (type)
            {
                case TurnSystemType.Classic: return "ClassicBattleController";
                case TurnSystemType.ATB: return "ATBBattleController";
                case TurnSystemType.Timeline: return "TimelineBattleController";
                case TurnSystemType.PressTurn: return "PressTurnBattleController";
                case TurnSystemType.ActionPoint: return "ActionPointBattleController";
                default: return "BattleController";
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  Code Generation
        // ════════════════════════════════════════════════════════════════

        private static string GenerateTemplate(TurnSystemType type, string className, string ns)
        {
            var sb = new StringBuilder();
            bool hasNs = !string.IsNullOrEmpty(ns);
            string t = hasNs ? "    " : "";

            AppendHeader(sb, className);
            AppendUsings(sb);
            sb.AppendLine();

            if (hasNs) { sb.AppendLine($"namespace {ns}"); sb.AppendLine("{"); }

            sb.AppendLine($"{t}public class {className} : MonoBehaviour");
            sb.AppendLine($"{t}{{");

            AppendFields(sb, t, type);
            sb.AppendLine();
            AppendSetup(sb, t, type);
            sb.AppendLine();

            switch (type)
            {
                case TurnSystemType.Classic: AppendClassicLoop(sb, t); break;
                case TurnSystemType.ATB: AppendATBLoop(sb, t); break;
                case TurnSystemType.Timeline: AppendTimelineLoop(sb, t); break;
                case TurnSystemType.PressTurn: AppendPressTurnLoop(sb, t); break;
                case TurnSystemType.ActionPoint: AppendActionPointLoop(sb, t); break;
            }

            sb.AppendLine();
            AppendPlayerActions(sb, t, type);
            sb.AppendLine();
            AppendEnemyAI(sb, t);
            sb.AppendLine();
            AppendGameLogic(sb, t);
            sb.AppendLine();
            AppendEventHandlers(sb, t);
            sb.AppendLine();
            AppendQueries(sb, t);
            sb.AppendLine();
            AppendUtility(sb, t, type);

            sb.AppendLine();
            AppendMockCombatant(sb, t);
            sb.AppendLine($"{t}}}");
            if (hasNs) sb.AppendLine("}");

            return sb.ToString();
        }

        // ── Header ──

        private static void AppendHeader(StringBuilder sb, string className)
        {
            sb.AppendLine("// \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550");
            sb.AppendLine($"// {className} \u2014 Generated by Simple JRPG Template Generator");
            sb.AppendLine("// This file is yours. Modify freely.");
            sb.AppendLine("// \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550");
        }

        // ── Usings ──

        private static void AppendUsings(StringBuilder sb)
        {
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using SimpleJRPG;");
        }

        // ── Fields ──

        private static void AppendFields(StringBuilder sb, string t, TurnSystemType type)
        {
            string i = t + "    ";

            sb.AppendLine($"{i}// ── Core ──");
            sb.AppendLine($"{i}private Battle _battle;");

            switch (type)
            {
                case TurnSystemType.Classic:
                    sb.AppendLine($"{i}private ClassicTurnSystem _classic;");
                    break;
                case TurnSystemType.ATB:
                    sb.AppendLine($"{i}private ATBTurnSystem _atb;");
                    break;
                case TurnSystemType.Timeline:
                    sb.AppendLine($"{i}private TimelineTurnSystem _timeline;");
                    break;
                case TurnSystemType.PressTurn:
                    sb.AppendLine($"{i}private PressTurnSystem _pts;");
                    break;
                case TurnSystemType.ActionPoint:
                    sb.AppendLine($"{i}private ActionPointTurnSystem _apts;");
                    break;
            }

            sb.AppendLine($"{i}private List<ICombatant> _party;");
            sb.AppendLine($"{i}private List<ICombatant> _enemies;");
        }

        // ── Setup (Start) ──

        private static void AppendSetup(StringBuilder sb, string t, TurnSystemType type)
        {
            string i = t + "    ";
            string i2 = i + "    ";

            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine($"{i}//  Setup");
            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"{i}private void Start()");
            sb.AppendLine($"{i}{{");

            // Combatants
            sb.AppendLine($"{i2}// TODO: Replace MockCombatant with your own ICombatant class");
            sb.AppendLine($"{i2}_party = new List<ICombatant>");
            sb.AppendLine($"{i2}{{");
            sb.AppendLine($"{i2}    new MockCombatant(\"Hero\", 100, 30, 10, 0),");
            sb.AppendLine($"{i2}    new MockCombatant(\"Warrior\", 120, 10, 8, 0),");
            sb.AppendLine($"{i2}    new MockCombatant(\"Mage\", 60, 50, 12, 0),");
            sb.AppendLine($"{i2}    new MockCombatant(\"Priest\", 80, 40, 9, 0),");
            sb.AppendLine($"{i2}}};");
            sb.AppendLine();
            sb.AppendLine($"{i2}_enemies = new List<ICombatant>");
            sb.AppendLine($"{i2}{{");
            sb.AppendLine($"{i2}    new MockCombatant(\"Blob\", 40, 0, 5, 1),");
            sb.AppendLine($"{i2}    new MockCombatant(\"Imp\", 30, 10, 11, 1),");
            sb.AppendLine($"{i2}    new MockCombatant(\"Mimic\", 80, 20, 7, 1),");
            sb.AppendLine($"{i2}}};");
            sb.AppendLine();

            sb.AppendLine($"{i2}var all = new List<ICombatant>();");
            sb.AppendLine($"{i2}all.AddRange(_party);");
            sb.AppendLine($"{i2}all.AddRange(_enemies);");
            sb.AppendLine();

            // Turn system
            switch (type)
            {
                case TurnSystemType.Classic:
                    sb.AppendLine($"{i2}_classic = new ClassicTurnSystem();");
                    sb.AppendLine($"{i2}_battle = new Battle();");
                    break;
                case TurnSystemType.ATB:
                    sb.AppendLine($"{i2}_atb = new ATBTurnSystem(100f);");
                    sb.AppendLine($"{i2}_battle = new Battle();");
                    break;
                case TurnSystemType.Timeline:
                    sb.AppendLine($"{i2}_timeline = new TimelineTurnSystem(100, 100);");
                    sb.AppendLine($"{i2}_battle = new Battle();");
                    break;
                case TurnSystemType.PressTurn:
                    sb.AppendLine($"{i2}_pts = new PressTurnSystem();");
                    sb.AppendLine($"{i2}_battle = new Battle();");
                    break;
                case TurnSystemType.ActionPoint:
                    sb.AppendLine($"{i2}_apts = new ActionPointTurnSystem(0, -4, 4);");
                    sb.AppendLine($"{i2}_battle = new Battle();");
                    break;
            }
            sb.AppendLine();

            // Subscribe to events
            sb.AppendLine($"{i2}// Subscribe to events");
            sb.AppendLine($"{i2}_battle.OnBattleStart += HandleBattleStart;");
            sb.AppendLine($"{i2}_battle.OnBattleEnd += HandleBattleEnd;");
            sb.AppendLine($"{i2}_battle.OnTurnStart += HandleTurnStart;");
            sb.AppendLine($"{i2}_battle.OnTurnEnd += HandleTurnEnd;");
            sb.AppendLine($"{i2}_battle.OnBeforeDamage += HandleBeforeDamage;");
            sb.AppendLine($"{i2}_battle.OnDamageDealt += HandleDamageDealt;");
            sb.AppendLine($"{i2}_battle.OnBeforeHeal += HandleBeforeHeal;");
            sb.AppendLine($"{i2}_battle.OnHealed += HandleHealed;");
            sb.AppendLine($"{i2}_battle.OnKO += HandleKO;");
            sb.AppendLine($"{i2}_battle.OnRevived += HandleRevived;");
            sb.AppendLine($"{i2}_battle.OnStatusApplied += HandleStatusApplied;");
            sb.AppendLine($"{i2}_battle.OnStatusRemoved += HandleStatusRemoved;");
            sb.AppendLine($"{i2}_battle.OnBuffApplied += HandleBuffApplied;");
            sb.AppendLine($"{i2}_battle.OnBuffRemoved += HandleBuffRemoved;");
            sb.AppendLine($"{i2}_battle.OnFled += HandleFled;");
            sb.AppendLine($"{i2}_battle.OnGroupDamageDealt += HandleGroupDamageDealt;");
            sb.AppendLine($"{i2}_battle.OnGroupHealed += HandleGroupHealed;");
            sb.AppendLine($"{i2}_battle.OnGroupStatusApplied += HandleGroupStatusApplied;");
            sb.AppendLine($"{i2}_battle.OnGroupBuffApplied += HandleGroupBuffApplied;");
            sb.AppendLine($"{i2}_battle.OnCombatantRemoved += HandleCombatantRemoved;");
            sb.AppendLine();

            // Start battle
            string tsVar = GetTurnSystemVar(type);
            sb.AppendLine($"{i2}_battle.Start(all, {tsVar});");

            // Initial turn
            switch (type)
            {
                case TurnSystemType.Classic:
                    sb.AppendLine($"{i2}NextTurn();");
                    break;
                case TurnSystemType.ATB:
                    sb.AppendLine($"{i2}// ATB: gauges fill in Update(), no initial NextTurn needed");
                    break;
                case TurnSystemType.Timeline:
                    sb.AppendLine($"{i2}NextTurn();");
                    break;
                case TurnSystemType.PressTurn:
                    sb.AppendLine($"{i2}// Press Turn: starts on team 0 (player) automatically");
                    sb.AppendLine($"{i2}NextPressTurnAction();");
                    break;
                case TurnSystemType.ActionPoint:
                    sb.AppendLine($"{i2}NextTurn();");
                    break;
            }

            sb.AppendLine($"{i}}}");
        }

        // ── Classic Loop ──

        private static void AppendClassicLoop(StringBuilder sb, string t)
        {
            string i = t + "    ";
            string i2 = i + "    ";
            string i3 = i2 + "    ";

            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine($"{i}//  Battle Loop — Classic (speed-sorted round-robin)");
            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"{i}private void NextTurn()");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}if (!IsActive()) return;");
            sb.AppendLine();
            sb.AppendLine($"{i2}var actor = _battle.BeginNextTurn();");
            sb.AppendLine($"{i2}if (actor == null) return;");
            sb.AppendLine();
            sb.AppendLine($"{i2}if (actor.Team == 0)");
            sb.AppendLine($"{i2}{{");
            sb.AppendLine($"{i3}// TODO: Show your command menu for this actor");
            sb.AppendLine($"{i3}Debug.Log($\"{{actor.Name}}'s turn — waiting for player input\");");
            sb.AppendLine($"{i3}// Call PlayerAttack(), PlayerHeal(), etc. from your UI");
            sb.AppendLine($"{i2}}}");
            sb.AppendLine($"{i2}else");
            sb.AppendLine($"{i2}{{");
            sb.AppendLine($"{i3}EnemyAction(actor);");
            sb.AppendLine($"{i3}EndTurnAndAdvance();");
            sb.AppendLine($"{i2}}}");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();
            sb.AppendLine($"{i}private void EndTurnAndAdvance()");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}_battle.EndTurn();");
            sb.AppendLine($"{i2}NextTurn();");
            sb.AppendLine($"{i}}}");
        }

        // ── ATB Loop ──

        private static void AppendATBLoop(StringBuilder sb, string t)
        {
            string i = t + "    ";
            string i2 = i + "    ";
            string i3 = i2 + "    ";
            string i4 = i3 + "    ";

            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine($"{i}//  Battle Loop — ATB (gauge-based, real-time)");
            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"{i}private void Update()");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}if (!IsActive()) return;");
            sb.AppendLine();
            sb.AppendLine($"{i2}// Tick all gauges");
            sb.AppendLine($"{i2}_atb.Tick(Time.deltaTime);");
            sb.AppendLine();
            sb.AppendLine($"{i2}// Process ready actors");
            sb.AppendLine($"{i2}while (_atb.HasReadyActor())");
            sb.AppendLine($"{i2}{{");
            sb.AppendLine($"{i3}var actor = _battle.BeginNextTurn();");
            sb.AppendLine($"{i3}if (actor == null) break;");
            sb.AppendLine();
            sb.AppendLine($"{i3}if (actor.Team == 0)");
            sb.AppendLine($"{i3}{{");
            sb.AppendLine($"{i4}// TODO: Show command menu for this party member");
            sb.AppendLine($"{i4}Debug.Log($\"{{actor.Name}} is ready! Gauge: {{_atb.GetGauge(actor):F0}}\");");
            sb.AppendLine($"{i4}// Call PlayerAttack(), PlayerHeal(), etc. from your UI");
            sb.AppendLine($"{i4}break; // Wait for player input");
            sb.AppendLine($"{i3}}}");
            sb.AppendLine($"{i3}else");
            sb.AppendLine($"{i3}{{");
            sb.AppendLine($"{i4}EnemyAction(actor);");
            sb.AppendLine($"{i4}_battle.EndTurn();");
            sb.AppendLine($"{i3}}}");
            sb.AppendLine($"{i2}}}");
            sb.AppendLine($"{i}}}");
        }

        // ── Timeline Loop ──

        private static void AppendTimelineLoop(StringBuilder sb, string t)
        {
            string i = t + "    ";
            string i2 = i + "    ";
            string i3 = i2 + "    ";

            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine($"{i}//  Battle Loop — Timeline / CTB (charge-time based)");
            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"{i}// Action cost constants — TODO: tune these for your game");
            sb.AppendLine($"{i}private const int CostAttack = 100;");
            sb.AppendLine($"{i}private const int CostHeal = 80;");
            sb.AppendLine($"{i}private const int CostDefend = 50;");
            sb.AppendLine($"{i}private const int CostSkill = 120;");
            sb.AppendLine();
            sb.AppendLine($"{i}private void NextTurn()");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}if (!IsActive()) return;");
            sb.AppendLine();
            sb.AppendLine($"{i2}var actor = _battle.BeginNextTurn();");
            sb.AppendLine($"{i2}if (actor == null) return;");
            sb.AppendLine();
            sb.AppendLine($"{i2}if (actor.Team == 0)");
            sb.AppendLine($"{i2}{{");
            sb.AppendLine($"{i3}// TODO: Show your command menu for this actor");
            sb.AppendLine($"{i3}Debug.Log($\"{{actor.Name}}'s turn (tick: {{_timeline.GetTick(actor)}})\");");
            sb.AppendLine($"{i3}// Call PlayerAttack(), PlayerHeal(), etc. from your UI");
            sb.AppendLine($"{i2}}}");
            sb.AppendLine($"{i2}else");
            sb.AppendLine($"{i2}{{");
            sb.AppendLine($"{i3}EnemyAction(actor);");
            sb.AppendLine($"{i3}EndTurnAndAdvance(CostAttack);");
            sb.AppendLine($"{i2}}}");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();
            sb.AppendLine($"{i}private void EndTurnAndAdvance(int actionCost)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}_timeline.SetActionCost(actionCost);");
            sb.AppendLine($"{i2}_battle.EndTurn();");
            sb.AppendLine($"{i2}NextTurn();");
            sb.AppendLine($"{i}}}");
        }

        // ── Press Turn Loop ──

        private static void AppendPressTurnLoop(StringBuilder sb, string t)
        {
            string i = t + "    ";
            string i2 = i + "    ";
            string i3 = i2 + "    ";

            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine($"{i}//  Battle Loop — Press Turn (team phases, point manipulation)");
            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"{i}private void NextPressTurnAction()");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}if (!IsActive()) return;");
            sb.AppendLine();
            sb.AppendLine($"{i2}if (!_pts.HasActionsRemaining)");
            sb.AppendLine($"{i2}{{");
            sb.AppendLine($"{i3}// Phase ended — EndTurn advances to the next team internally");
            sb.AppendLine($"{i3}_battle.EndTurn();");
            sb.AppendLine($"{i3}Debug.Log($\"Team {{_pts.ActiveTeam}} phase — Full: {{_pts.FullPoints}}, Half: {{_pts.HalfPoints}}\");");
            sb.AppendLine($"{i2}}}");
            sb.AppendLine();
            sb.AppendLine($"{i2}if (_pts.ActiveTeam == 0)");
            sb.AppendLine($"{i2}{{");
            sb.AppendLine($"{i3}// TODO: Let the player pick which party member acts");
            sb.AppendLine($"{i3}Debug.Log($\"Player phase — Full: {{_pts.FullPoints}}, Half: {{_pts.HalfPoints}}\");");
            sb.AppendLine($"{i3}// Call SelectAndAct(partyMember) from your UI");
            sb.AppendLine($"{i2}}}");
            sb.AppendLine($"{i2}else");
            sb.AppendLine($"{i2}{{");
            sb.AppendLine($"{i3}// Enemy phase — auto-pick and act");
            sb.AppendLine($"{i3}EnemyPressTurnPhase();");
            sb.AppendLine($"{i2}}}");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // SelectAndAct
            sb.AppendLine($"{i}/// <summary>Call this from your UI when the player picks an actor.</summary>");
            sb.AppendLine($"{i}public void SelectAndAct(ICombatant actor)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}_pts.SelectActor(actor);");
            sb.AppendLine($"{i2}_battle.BeginNextTurn();");
            sb.AppendLine($"{i2}// TODO: Show command menu for this actor");
            sb.AppendLine($"{i2}Debug.Log($\"Selected {{actor.Name}} — choose an action\");");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // EndPressTurnAction
            sb.AppendLine($"{i}private void EndPressTurnAction(bool convertInsteadOfConsume = false)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}if (convertInsteadOfConsume)");
            sb.AppendLine($"{i3}_pts.ConvertAction();");
            sb.AppendLine($"{i2}else");
            sb.AppendLine($"{i3}_pts.ConsumeAction();");
            sb.AppendLine();
            sb.AppendLine($"{i2}_battle.EndTurn();");
            sb.AppendLine($"{i2}NextPressTurnAction();");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // Enemy phase
            sb.AppendLine($"{i}private void EnemyPressTurnPhase()");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}while (_pts.HasActionsRemaining && IsActive())");
            sb.AppendLine($"{i2}{{");
            sb.AppendLine($"{i3}var enemies = _battle.GetAlive(_pts.ActiveTeam);");
            sb.AppendLine($"{i3}if (enemies.Count == 0) break;");
            sb.AppendLine();
            sb.AppendLine($"{i3}var actor = enemies[UnityEngine.Random.Range(0, enemies.Count)];");
            sb.AppendLine($"{i3}_pts.SelectActor(actor);");
            sb.AppendLine($"{i3}_battle.BeginNextTurn();");
            sb.AppendLine($"{i3}EnemyAction(actor);");
            sb.AppendLine($"{i3}_pts.ConsumeAction();");
            sb.AppendLine($"{i3}_battle.EndTurn();");
            sb.AppendLine($"{i2}}}");
            sb.AppendLine();
            sb.AppendLine($"{i2}// Enemy phase done — advance to player");
            sb.AppendLine($"{i2}NextPressTurnAction();");
            sb.AppendLine($"{i}}}");
        }

        // ── Action Point Loop ──

        private static void AppendActionPointLoop(StringBuilder sb, string t)
        {
            string i = t + "    ";
            string i2 = i + "    ";
            string i3 = i2 + "    ";

            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine($"{i}//  Battle Loop — Action Point (save or spend AP for extra actions)");
            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"{i}private void NextTurn()");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}if (!IsActive()) return;");
            sb.AppendLine();
            sb.AppendLine($"{i2}var actor = _battle.BeginNextTurn();");
            sb.AppendLine($"{i2}if (actor == null) return;");
            sb.AppendLine();
            sb.AppendLine($"{i2}if (actor.Team == 0)");
            sb.AppendLine($"{i2}{{");
            sb.AppendLine($"{i3}// TODO: Show your command menu + AP display");
            sb.AppendLine($"{i3}Debug.Log($\"{{actor.Name}}'s turn — AP: {{_apts.GetAP(actor)}} (range: {{_apts.MinAP}} to {{_apts.MaxAP}})\");");
            sb.AppendLine($"{i3}// Call PlayerAttack(), PlayerSaveAP(), PlayerSpendAP(), etc. from your UI");
            sb.AppendLine($"{i2}}}");
            sb.AppendLine($"{i2}else");
            sb.AppendLine($"{i2}{{");
            sb.AppendLine($"{i3}EnemyAction(actor);");
            sb.AppendLine($"{i3}EndTurnAndAdvance();");
            sb.AppendLine($"{i2}}}");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();
            sb.AppendLine($"{i}private void EndTurnAndAdvance()");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}_battle.EndTurn();");
            sb.AppendLine($"{i2}NextTurn();");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // AP-specific actions
            sb.AppendLine($"{i}/// <summary>Spend 1 AP to gain an extra action this turn.</summary>");
            sb.AppendLine($"{i}public void PlayerSpendAP()");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}_apts.SpendAP();");
            sb.AppendLine($"{i2}Debug.Log($\"Spent AP! Remaining: {{_apts.GetAP(_battle.CurrentActor)}}\");");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();
            sb.AppendLine($"{i}/// <summary>Skip this turn and bank +1 AP for later.</summary>");
            sb.AppendLine($"{i}public void PlayerSaveAP()");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}_apts.SaveAP();");
            sb.AppendLine($"{i2}Debug.Log($\"Saved AP! Balance: {{_apts.GetAP(_battle.CurrentActor)}}\");");
            sb.AppendLine($"{i2}EndTurnAndAdvance();");
            sb.AppendLine($"{i}}}");
        }

        // ── Player Actions ──

        private static void AppendPlayerActions(StringBuilder sb, string t, TurnSystemType type)
        {
            string i = t + "    ";
            string i2 = i + "    ";
            string i3 = i2 + "    ";

            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine($"{i}//  Player Actions — call these from your UI buttons");
            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine();

            string endTurnCall;
            switch (type)
            {
                case TurnSystemType.Timeline:
                    endTurnCall = "EndTurnAndAdvance(CostAttack);";
                    break;
                case TurnSystemType.PressTurn:
                    endTurnCall = "EndPressTurnAction();";
                    break;
                default:
                    endTurnCall = "EndTurnAndAdvance();";
                    break;
            }

            string endTurnHeal;
            switch (type)
            {
                case TurnSystemType.Timeline:
                    endTurnHeal = "EndTurnAndAdvance(CostHeal);";
                    break;
                case TurnSystemType.PressTurn:
                    endTurnHeal = "EndPressTurnAction();";
                    break;
                default:
                    endTurnHeal = "EndTurnAndAdvance();";
                    break;
            }

            string endTurnDefend;
            switch (type)
            {
                case TurnSystemType.Timeline:
                    endTurnDefend = "EndTurnAndAdvance(CostDefend);";
                    break;
                case TurnSystemType.PressTurn:
                    endTurnDefend = "EndPressTurnAction();";
                    break;
                default:
                    endTurnDefend = "EndTurnAndAdvance();";
                    break;
            }

            // ATB doesn't have EndTurnAndAdvance, it uses _battle.EndTurn() directly
            if (type == TurnSystemType.ATB)
            {
                endTurnCall = "_battle.EndTurn();";
                endTurnHeal = "_battle.EndTurn();";
                endTurnDefend = "_battle.EndTurn();";
            }

            // Attack
            sb.AppendLine($"{i}public void PlayerAttack()");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}var actor = _battle.CurrentActor;");
            sb.AppendLine($"{i2}var target = PickTarget(actor, _battle.GetEnemies(actor));");
            sb.AppendLine($"{i2}int damage = CalculateDamage(actor, target);");
            sb.AppendLine($"{i2}_battle.DealDamage(actor, target, damage, \"physical\", \"\");");
            sb.AppendLine($"{i2}{endTurnCall}");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // Heal
            sb.AppendLine($"{i}public void PlayerHeal()");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}var actor = _battle.CurrentActor;");
            sb.AppendLine($"{i2}// TODO: Let the player pick an ally to heal");
            sb.AppendLine($"{i2}var target = PickTarget(actor, _battle.GetAllies(actor, true));");
            sb.AppendLine($"{i2}int amount = CalculateHeal(actor, target);");
            sb.AppendLine($"{i2}_battle.Heal(actor, target, amount);");
            sb.AppendLine($"{i2}{endTurnHeal}");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // Revive
            sb.AppendLine($"{i}public void PlayerRevive()");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}var actor = _battle.CurrentActor;");
            sb.AppendLine($"{i2}var dead = _battle.GetDead(actor.Team);");
            sb.AppendLine($"{i2}if (dead.Count == 0) return;");
            sb.AppendLine($"{i2}// TODO: Let the player pick who to revive");
            sb.AppendLine($"{i2}_battle.Revive(dead[0], 1); // TODO: revive HP amount");
            sb.AppendLine($"{i2}{endTurnHeal}");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // Defend
            sb.AppendLine($"{i}public void PlayerDefend()");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}var actor = _battle.CurrentActor;");
            sb.AppendLine($"{i2}// TODO: Apply a defense buff, reduce incoming damage, etc.");
            sb.AppendLine($"{i2}_battle.ApplyBuff(actor, actor, \"defend\", 0.5f, 1);");
            sb.AppendLine($"{i2}{endTurnDefend}");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // Flee
            sb.AppendLine($"{i}public void PlayerFlee()");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}float chance = GetFleeChance();");
            sb.AppendLine($"{i2}if (UnityEngine.Random.value < chance)");
            sb.AppendLine($"{i2}{{");
            sb.AppendLine($"{i3}_battle.Flee(_battle.CurrentActor);");
            sb.AppendLine($"{i2}}}");
            sb.AppendLine($"{i2}else");
            sb.AppendLine($"{i2}{{");
            sb.AppendLine($"{i3}Debug.Log(\"Failed to flee!\");");
            sb.AppendLine($"{i3}{endTurnDefend}");
            sb.AppendLine($"{i2}}}");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // Apply Status
            sb.AppendLine($"{i}public void PlayerApplyStatus()");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}var actor = _battle.CurrentActor;");
            sb.AppendLine($"{i2}var target = PickTarget(actor, _battle.GetEnemies(actor));");
            sb.AppendLine($"{i2}// TODO: Replace with your status ID and duration");
            sb.AppendLine($"{i2}_battle.ApplyStatus(actor, target, \"poison\", 3);");
            sb.AppendLine($"{i2}{endTurnCall}");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // Apply Buff
            sb.AppendLine($"{i}public void PlayerApplyBuff()");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}var actor = _battle.CurrentActor;");
            sb.AppendLine($"{i2}var target = PickTarget(actor, _battle.GetAllies(actor, true));");
            sb.AppendLine($"{i2}// TODO: Replace with your buff ID, amount, and duration");
            sb.AppendLine($"{i2}_battle.ApplyBuff(actor, target, \"attack_up\", 1.25f, 3);");
            sb.AppendLine($"{i2}{endTurnCall}");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // Multi-Target
            sb.AppendLine($"{i}public void PlayerMultiTarget()");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}var actor = _battle.CurrentActor;");
            sb.AppendLine($"{i2}var targets = _battle.GetEnemies(actor);");
            sb.AppendLine($"{i2}int damage = CalculateDamage(actor, targets[0]); // TODO: per-target damage");
            sb.AppendLine($"{i2}_battle.DealDamage(actor, targets, damage, \"magical\", \"fire\");");
            sb.AppendLine($"{i2}{endTurnCall}");
            sb.AppendLine($"{i}}}");
        }

        // ── Enemy AI ──

        private static void AppendEnemyAI(StringBuilder sb, string t)
        {
            string i = t + "    ";
            string i2 = i + "    ";

            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine($"{i}//  Enemy AI");
            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"{i}private void EnemyAction(ICombatant actor)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Implement your enemy AI — skill selection, targeting, etc.");
            sb.AppendLine($"{i2}var targets = _battle.GetEnemies(actor);");
            sb.AppendLine($"{i2}if (targets.Count == 0) return;");
            sb.AppendLine();
            sb.AppendLine($"{i2}var target = PickTarget(actor, targets);");
            sb.AppendLine($"{i2}int damage = CalculateDamage(actor, target);");
            sb.AppendLine($"{i2}_battle.DealDamage(actor, target, damage, \"physical\", \"\");");
            sb.AppendLine($"{i}}}");
        }

        // ── Game Logic ──

        private static void AppendGameLogic(StringBuilder sb, string t)
        {
            string i = t + "    ";
            string i2 = i + "    ";

            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine($"{i}//  Game Logic — placeholder formulas, replace with yours");
            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine();

            // CalculateDamage
            sb.AppendLine($"{i}private int CalculateDamage(ICombatant attacker, ICombatant target)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Your damage formula (e.g. attack - defense, elemental multipliers, crits)");
            sb.AppendLine($"{i2}return 10;");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // CalculateHeal
            sb.AppendLine($"{i}private int CalculateHeal(ICombatant healer, ICombatant target)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Your heal formula (e.g. magic power * spell multiplier)");
            sb.AppendLine($"{i2}return 20;");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // PickTarget
            sb.AppendLine($"{i}private ICombatant PickTarget(ICombatant actor, List<ICombatant> candidates)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Your targeting logic (e.g. lowest HP, random, player choice)");
            sb.AppendLine($"{i2}if (candidates.Count == 0) return null;");
            sb.AppendLine($"{i2}return candidates[UnityEngine.Random.Range(0, candidates.Count)];");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // CanAffordSkill
            sb.AppendLine($"{i}private bool CanAffordSkill(ICombatant actor, int mpCost)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Check your MP/resource system");
            sb.AppendLine($"{i2}return true;");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // GetFleeChance
            sb.AppendLine($"{i}private float GetFleeChance()");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Your flee formula (e.g. party avg speed vs enemy avg speed)");
            sb.AppendLine($"{i2}return 0.5f;");
            sb.AppendLine($"{i}}}");
        }

        // ── Event Handlers ──

        private static void AppendEventHandlers(StringBuilder sb, string t)
        {
            string i = t + "    ";
            string i2 = i + "    ";

            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine($"{i}//  Event Handlers — hook up your UI, VFX, audio here");
            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine();

            // OnBattleStart
            sb.AppendLine($"{i}private void HandleBattleStart(Battle battle)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Show battle intro, play music, etc.");
            sb.AppendLine($"{i2}Debug.Log(\"Battle started!\");");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // OnBattleEnd
            sb.AppendLine($"{i}private void HandleBattleEnd(Battle battle, BattleState result)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Show victory/defeat screen, award XP, etc.");
            sb.AppendLine($"{i2}Debug.Log($\"Battle ended: {{result}}\");");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // OnTurnStart
            sb.AppendLine($"{i}private void HandleTurnStart(TurnEvent evt)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Highlight active character, show turn indicator");
            sb.AppendLine($"{i2}Debug.Log($\"{{evt.Actor.Name}}'s turn begins (turn {{evt.TurnNumber}})\");");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // OnTurnEnd
            sb.AppendLine($"{i}private void HandleTurnEnd(TurnEvent evt)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Tick status durations, remove expired buffs, etc.");
            sb.AppendLine($"{i2}Debug.Log($\"{{evt.Actor.Name}}'s turn ends\");");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // OnBeforeDamage
            sb.AppendLine($"{i}private void HandleBeforeDamage(BeforeDamageEvent evt)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Modify damage, apply shields, cancel attacks");
            sb.AppendLine($"{i2}// Example: if (targetHasShield) evt.Cancel = true;");
            sb.AppendLine($"{i2}// Example: if (evt.Element == \"fire\" && targetWeakToFire) evt.Amount *= 2;");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // OnDamageDealt
            sb.AppendLine($"{i}private void HandleDamageDealt(DamageEvent evt)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Play hit animation, show damage number, screen shake");
            sb.AppendLine($"{i2}Debug.Log($\"{{evt.Source.Name}} deals {{evt.Amount}} damage to {{evt.Target.Name}}\");");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // OnBeforeHeal
            sb.AppendLine($"{i}private void HandleBeforeHeal(BeforeHealEvent evt)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Modify heal amount, apply heal boost buffs");
            sb.AppendLine($"{i2}// Example: if (healerHasHealBoost) evt.Amount = (int)(evt.Amount * 1.5f);");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // OnHealed
            sb.AppendLine($"{i}private void HandleHealed(HealEvent evt)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Play heal animation, show heal number, update HP bar");
            sb.AppendLine($"{i2}Debug.Log($\"{{evt.Source.Name}} heals {{evt.Target.Name}} for {{evt.Amount}}\");");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // OnKO
            sb.AppendLine($"{i}private void HandleKO(KOEvent evt)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Play death animation, grey out portrait, etc.");
            sb.AppendLine($"{i2}Debug.Log($\"{{evt.Target.Name}} defeated!\");");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // OnRevived
            sb.AppendLine($"{i}private void HandleRevived(ICombatant target)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Play revive animation, restore portrait");
            sb.AppendLine($"{i2}Debug.Log($\"{{target.Name}} revived!\");");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // OnStatusApplied
            sb.AppendLine($"{i}private void HandleStatusApplied(StatusEvent evt)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Show status icon, play status animation");
            sb.AppendLine($"{i2}Debug.Log($\"{{evt.StatusId}} applied to {{evt.Target.Name}} for {{evt.Duration}} turns\");");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // OnStatusRemoved
            sb.AppendLine($"{i}private void HandleStatusRemoved(ICombatant target, string statusId)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Remove status icon");
            sb.AppendLine($"{i2}Debug.Log($\"{{statusId}} removed from {{target.Name}}\");");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // OnBuffApplied
            sb.AppendLine($"{i}private void HandleBuffApplied(BuffEvent evt)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Show buff icon, play buff animation");
            sb.AppendLine($"{i2}Debug.Log($\"{{evt.BuffId}} applied to {{evt.Target.Name}} (x{{evt.Amount}}) for {{evt.Duration}} turns\");");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // OnBuffRemoved
            sb.AppendLine($"{i}private void HandleBuffRemoved(ICombatant target, string buffId)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Remove buff icon");
            sb.AppendLine($"{i2}Debug.Log($\"{{buffId}} removed from {{target.Name}}\");");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // OnFled
            sb.AppendLine($"{i}private void HandleFled(ICombatant who)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Play flee animation, return to overworld");
            sb.AppendLine($"{i2}Debug.Log($\"{{who.Name}} fled from battle!\");");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // OnGroupDamageDealt
            sb.AppendLine($"{i}private void HandleGroupDamageDealt(GroupDamageEvent evt)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Play AoE animation, show total damage");
            sb.AppendLine($"{i2}Debug.Log($\"Hit {{evt.Hits.Count}} targets for {{evt.TotalDamage}} total damage\");");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // OnGroupHealed
            sb.AppendLine($"{i}private void HandleGroupHealed(GroupHealEvent evt)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Play group heal animation");
            sb.AppendLine($"{i2}Debug.Log($\"Healed {{evt.Heals.Count}} targets for {{evt.TotalHealed}} total\");");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // OnGroupStatusApplied
            sb.AppendLine($"{i}private void HandleGroupStatusApplied(GroupStatusEvent evt)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Play group status animation");
            sb.AppendLine($"{i2}Debug.Log($\"Applied status to {{evt.Applications.Count}} targets\");");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // OnGroupBuffApplied
            sb.AppendLine($"{i}private void HandleGroupBuffApplied(GroupBuffEvent evt)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Play group buff animation");
            sb.AppendLine($"{i2}Debug.Log($\"Applied buff to {{evt.Applications.Count}} targets\");");
            sb.AppendLine($"{i}}}");
            sb.AppendLine();

            // OnCombatantRemoved
            sb.AppendLine($"{i}private void HandleCombatantRemoved(ICombatant combatant)");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// TODO: Remove combatant from UI");
            sb.AppendLine($"{i2}Debug.Log($\"{{combatant.Name}} removed from battle\");");
            sb.AppendLine($"{i}}}");
        }

        // ── Queries ──

        private static void AppendQueries(StringBuilder sb, string t)
        {
            string i = t + "    ";
            string i2 = i + "    ";

            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine($"{i}//  Queries — useful helpers for UI and AI");
            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"{i}// These are examples of what's available. Use wherever you need them.");
            sb.AppendLine($"{i}private void LogBattleState()");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}// Alive/dead queries");
            sb.AppendLine($"{i2}var allAlive = _battle.GetAlive();          // all alive combatants");
            sb.AppendLine($"{i2}var partyAlive = _battle.GetAlive(0);       // alive party members");
            sb.AppendLine($"{i2}var allDead = _battle.GetDead();            // all dead combatants");
            sb.AppendLine();
            sb.AppendLine($"{i2}// Relationship queries");
            sb.AppendLine($"{i2}if (_battle.CurrentActor != null)");
            sb.AppendLine($"{i2}{{");
            sb.AppendLine($"{i2}    var enemies = _battle.GetEnemies(_battle.CurrentActor);");
            sb.AppendLine($"{i2}    var allies = _battle.GetAllies(_battle.CurrentActor, true);");
            sb.AppendLine($"{i2}}}");
            sb.AppendLine();
            sb.AppendLine($"{i2}// Turn order");
            sb.AppendLine($"{i2}var timeline = _battle.GetTimeline();");
            sb.AppendLine();
            sb.AppendLine($"{i2}// Battle history and result");
            sb.AppendLine($"{i2}var history = _battle.History;              // all recorded events");
            sb.AppendLine($"{i2}var result = _battle.Result;                // null until battle ends");
            sb.AppendLine($"{i2}var turnNumber = _battle.TurnNumber;");
            sb.AppendLine($"{i2}var state = _battle.State;");
            sb.AppendLine($"{i}}}");
        }

        // ── Utility ──

        private static void AppendUtility(StringBuilder sb, string t, TurnSystemType type)
        {
            string i = t + "    ";
            string i2 = i + "    ";

            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine($"{i}//  Utility");
            sb.AppendLine($"{i}// ════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"{i}private bool IsActive()");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}return _battle != null");
            sb.AppendLine($"{i2}    && _battle.State != BattleState.NotStarted");
            sb.AppendLine($"{i2}    && _battle.State != BattleState.Victory");
            sb.AppendLine($"{i2}    && _battle.State != BattleState.Defeat");
            sb.AppendLine($"{i2}    && _battle.State != BattleState.Fled;");
            sb.AppendLine($"{i}}}");

            // Turn-system-specific helpers
            switch (type)
            {
                case TurnSystemType.ATB:
                    sb.AppendLine();
                    sb.AppendLine($"{i}/// <summary>Get gauge fill percentage (0-1) for UI display.</summary>");
                    sb.AppendLine($"{i}public float GetGaugePercent(ICombatant combatant)");
                    sb.AppendLine($"{i}{{");
                    sb.AppendLine($"{i2}return _atb.GetGauge(combatant) / 100f;");
                    sb.AppendLine($"{i}}}");
                    break;

                case TurnSystemType.Timeline:
                    sb.AppendLine();
                    sb.AppendLine($"{i}/// <summary>Get the current tick for a combatant (lower = sooner).</summary>");
                    sb.AppendLine($"{i}public int GetTick(ICombatant combatant)");
                    sb.AppendLine($"{i}{{");
                    sb.AppendLine($"{i2}return _timeline.GetTick(combatant);");
                    sb.AppendLine($"{i}}}");
                    break;

                case TurnSystemType.PressTurn:
                    sb.AppendLine();
                    sb.AppendLine($"{i}/// <summary>Get current press turn point counts for UI display.</summary>");
                    sb.AppendLine($"{i}public (int full, int half) GetPoints()");
                    sb.AppendLine($"{i}{{");
                    sb.AppendLine($"{i2}return (_pts.FullPoints, _pts.HalfPoints);");
                    sb.AppendLine($"{i}}}");
                    break;

                case TurnSystemType.ActionPoint:
                    sb.AppendLine();
                    sb.AppendLine($"{i}/// <summary>Get current AP for a combatant for UI display.</summary>");
                    sb.AppendLine($"{i}public int GetAP(ICombatant combatant)");
                    sb.AppendLine($"{i}{{");
                    sb.AppendLine($"{i2}return _apts.GetAP(combatant);");
                    sb.AppendLine($"{i}}}");
                    break;
            }
        }

        // ── Inline MockCombatant ──

        private static void AppendMockCombatant(StringBuilder sb, string t)
        {
            string i = t + "    ";
            string i2 = i + "    ";
            string i3 = i2 + "    ";

            sb.AppendLine($"{i}// ═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"{i}//  TODO: Delete this once you have your own ICombatant class.");
            sb.AppendLine($"{i}// ═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"{i}private class MockCombatant : ICombatant");
            sb.AppendLine($"{i}{{");
            sb.AppendLine($"{i2}public string Name {{ get; }}");
            sb.AppendLine($"{i2}public bool IsAlive => HP > 0;");
            sb.AppendLine($"{i2}public int Team {{ get; }}");
            sb.AppendLine($"{i2}public float Speed {{ get; }}");
            sb.AppendLine($"{i2}public int HP {{ get; private set; }}");
            sb.AppendLine($"{i2}public int MaxHP {{ get; }}");
            sb.AppendLine($"{i2}public int MP {{ get; set; }}");
            sb.AppendLine($"{i2}public int MaxMP {{ get; }}");
            sb.AppendLine();
            sb.AppendLine($"{i2}public MockCombatant(string name, int hp, int mp, float speed, int team)");
            sb.AppendLine($"{i2}{{");
            sb.AppendLine($"{i3}Name = name;");
            sb.AppendLine($"{i3}HP = hp;");
            sb.AppendLine($"{i3}MaxHP = hp;");
            sb.AppendLine($"{i3}MP = mp;");
            sb.AppendLine($"{i3}MaxMP = mp;");
            sb.AppendLine($"{i3}Speed = speed;");
            sb.AppendLine($"{i3}Team = team;");
            sb.AppendLine($"{i2}}}");
            sb.AppendLine();
            sb.AppendLine($"{i2}public void TakeDamage(int amount) => HP = Math.Max(0, HP - amount);");
            sb.AppendLine($"{i2}public void Heal(int amount) => HP = Math.Min(MaxHP, HP + amount);");
            sb.AppendLine($"{i}}}");
        }

        private static string GetTurnSystemVar(TurnSystemType type)
        {
            switch (type)
            {
                case TurnSystemType.Classic: return "_classic";
                case TurnSystemType.ATB: return "_atb";
                case TurnSystemType.Timeline: return "_timeline";
                case TurnSystemType.PressTurn: return "_pts";
                case TurnSystemType.ActionPoint: return "_apts";
                default: return "_classic";
            }
        }
    }
}
