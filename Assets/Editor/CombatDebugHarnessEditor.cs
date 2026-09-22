using UnityEditor;
using UnityEngine;
using Core;
using Core.CombatSystem;
using CombatUnit = Core.CombatSystem.Unit.Unit;

namespace ExtendedDebug.Editor
{
    /// <summary>
    /// Shows the current roster, whose turn it is, and the upcoming turn order, and lets you
    /// drive every CombatController verb by hand. Read-only outside Play mode.
    /// </summary>
    [CustomEditor(typeof(CombatDebugHarness))]
    public class CombatDebugHarnessEditor : UnityEditor.Editor
    {
        private enum DebugAction
        {
            EndTurn, Kill, Revive, DealDamage, Heal, Flee,
            SpendSP, RestoreSP,
            Advance, Delay, MoveToFront, MoveToBack, Swap, InsertAfter, GrantExtraTurn
        }

        private DebugAction selectedAction;
        private string unitNameInput = "";
        private string secondaryUnitNameInput = "";
        private int amountInput = 1;
        private UNITY_TYPE damageTypeInput;

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
            DrawUpcoming(combat);
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
                string status = unit.IsAlive ? $"HP {unit.HP}/{unit.MaxHP} · SP {unit.SP}/{unit.MaxSP}" : "dead";
                EditorGUILayout.LabelField($"{marker}{unit.Name}  ·  {unit.Team}  ·  {status}");
            }
        }

        private void DrawUpcoming(CombatController combat)
        {
            EditorGUILayout.LabelField("Upcoming turn order", EditorStyles.boldLabel);

            var upcoming = combat.UpcomingTurns;
            if (upcoming.Count == 0)
            {
                EditorGUILayout.LabelField("(empty)");
                return;
            }

            for (int i = 0; i < upcoming.Count; i++)
            {
                CombatUnit unit = upcoming[i];
                EditorGUILayout.LabelField(unit != null ? $"{i}.  {unit.Name}" : $"{i}.  <null>");
            }
        }

        private void DrawControls(CombatDebugHarness harness, CombatController combat)
        {
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);

            selectedAction = (DebugAction)EditorGUILayout.EnumPopup("Action", selectedAction);

            bool needsUnit = selectedAction != DebugAction.EndTurn && selectedAction != DebugAction.Flee;
            if (needsUnit)
                unitNameInput = EditorGUILayout.TextField("Unit", unitNameInput);

            bool needsSecondaryUnit = selectedAction is DebugAction.Swap or DebugAction.InsertAfter;
            if (needsSecondaryUnit)
            {
                string label = selectedAction == DebugAction.Swap ? "Unit B" : "Insert after";
                secondaryUnitNameInput = EditorGUILayout.TextField(label, secondaryUnitNameInput);
            }

            bool needsAmount = selectedAction is DebugAction.Revive or DebugAction.DealDamage or DebugAction.Heal
                or DebugAction.SpendSP or DebugAction.RestoreSP or DebugAction.Advance or DebugAction.Delay;
            if (needsAmount)
            {
                string label = selectedAction switch
                {
                    DebugAction.Revive => "HP",
                    DebugAction.Advance or DebugAction.Delay => "N",
                    _ => "Amount"
                };
                amountInput = EditorGUILayout.IntField(label, amountInput);
            }

            if (selectedAction == DebugAction.DealDamage)
                damageTypeInput = (UNITY_TYPE)EditorGUILayout.EnumPopup("Damage type", damageTypeInput);

            if (selectedAction == DebugAction.Flee)
                EditorGUILayout.HelpBox("Attempts a flee for the current actor's team.", MessageType.None);

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

            if (selectedAction == DebugAction.Flee)
            {
                if (combat.CurrentActor == null)
                {
                    Debug.LogWarning("[CombatDebugHarnessEditor] No current actor to flee with.");
                    return;
                }

                combat.Flee(combat.CurrentActor.Team);
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
                    combat.Revive(unit, amountInput);
                    break;
                case DebugAction.DealDamage:
                    combat.DealDamage(null, unit, amountInput, damageTypeInput, "debug");
                    break;
                case DebugAction.Heal:
                    combat.Heal(null, unit, amountInput, "debug");
                    break;
                case DebugAction.SpendSP:
                {
                    bool spent = combat.SpendSP(unit, amountInput);
                    Debug.Log(spent
                        ? $"[Debug] Spent {amountInput} SP on {unit.Name}"
                        : $"[Debug] {unit.Name} doesn't have enough SP");
                    break;
                }
                case DebugAction.RestoreSP:
                    combat.RestoreSP(unit, amountInput);
                    break;
                case DebugAction.Advance:
                    combat.Advance(unit, amountInput);
                    break;
                case DebugAction.Delay:
                    combat.Delay(unit, amountInput);
                    break;
                case DebugAction.MoveToFront:
                    combat.MoveToFront(unit);
                    break;
                case DebugAction.MoveToBack:
                    combat.MoveToBack(unit);
                    break;
                case DebugAction.Swap:
                {
                    CombatUnit unitB = harness.FindUnit(secondaryUnitNameInput);
                    if (unitB == null) { Debug.LogWarning($"[CombatDebugHarnessEditor] No unit named '{secondaryUnitNameInput}' found."); return; }
                    combat.Swap(unit, unitB);
                    break;
                }
                case DebugAction.InsertAfter:
                {
                    CombatUnit after = harness.FindUnit(secondaryUnitNameInput);
                    if (after == null) { Debug.LogWarning($"[CombatDebugHarnessEditor] No unit named '{secondaryUnitNameInput}' found."); return; }
                    combat.InsertAfter(unit, after);
                    break;
                }
                case DebugAction.GrantExtraTurn:
                {
                    bool granted = combat.GrantExtraTurn(unit);
                    Debug.Log(granted
                        ? $"[Debug] Granted extra turn to {unit.Name}"
                        : $"[Debug] {unit.Name} already has one pending");
                    break;
                }
            }
        }
    }
}