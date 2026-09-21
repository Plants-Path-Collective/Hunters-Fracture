using UnityEditor;
using UnityEngine;
using ExtendedDebug;
using Core.CombatSystem;
using CombatUnit = Core.CombatSystem.Unit.Unit;

namespace Editor
{
    /// <summary>
    /// Shows the current roster and whose turn it is, and lets you drive combat by hand:
    /// End Turn, Kill a unit by name, or Revive one. Read-only outside Play mode.
    /// </summary>
    [CustomEditor(typeof(CombatDebugHarness))]
    public class CombatDebugHarnessEditor : UnityEditor.Editor
    {
        private enum DebugAction { EndTurn, Kill, Revive }

        private DebugAction selectedAction;
        private string unitNameInput = "";
        private int reviveHpInput = 1;

        private void OnInspectorUpdate() => Repaint();

        public override void OnInspectorGUI()
        {
            var harness = (CombatDebugHarness)target;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play mode to see and drive combat.", MessageType.Info);
                return;
            }

            CombatController combat = harness.Combat;
            if (combat == null)
            {
                EditorGUILayout.HelpBox("No CombatController found.", MessageType.Warning);
                return;
            }

            DrawStatus(combat);
            EditorGUILayout.Space();
            DrawRoster(combat);
            EditorGUILayout.Space();
            DrawControls(harness, combat);
        }

        private void DrawStatus(CombatController combat)
        {
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Active", combat.IsCombatActive.ToString());
            EditorGUILayout.LabelField("Current actor", combat.CurrentActor != null ? combat.CurrentActor.Name : "—");
        }

        private void DrawRoster(CombatController combat)
        {
            EditorGUILayout.LabelField("Roster", EditorStyles.boldLabel);

            foreach (CombatUnit unit in combat.Roster)
            {
                if (unit == null) continue;

                string marker = unit == combat.CurrentActor ? "→ " : "   ";
                string status = unit.IsAlive ? $"HP {unit.HP}" : "dead";
                EditorGUILayout.LabelField($"{marker}{unit.Name}  ·  {unit.Team}  ·  {status}");
            }
        }

        private void DrawControls(CombatDebugHarness harness, CombatController combat)
        {
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);

            selectedAction = (DebugAction)EditorGUILayout.EnumPopup("Action", selectedAction);

            if (selectedAction != DebugAction.EndTurn)
                unitNameInput = EditorGUILayout.TextField("Unit", unitNameInput);

            if (selectedAction == DebugAction.Revive)
                reviveHpInput = EditorGUILayout.IntField("HP", reviveHpInput);

            if (GUILayout.Button("Apply"))
                Apply(harness, combat);
        }

        private void Apply(CombatDebugHarness harness, CombatController combat)
        {
            if (selectedAction == DebugAction.EndTurn)
            {
                combat.EndTurn();
                return;
            }

            CombatUnit unit = harness.FindUnit(unitNameInput);
            if (unit == null)
            {
                Debug.LogWarning($"[CombatDebugHarnessEditor] No unit named '{unitNameInput}' found.");
                return;
            }

            switch (selectedAction)
            {
                case DebugAction.Kill:
                    combat.Kill(unit);
                    break;
                case DebugAction.Revive:
                    combat.Revive(unit, reviveHpInput);
                    break;
            }
        }
    }
}