using System.Linq;
using UnityEditor;
using UnityEngine;
using Core.CombatSystem;
using CombatUnit = Core.CombatSystem.Unit.Unit;

namespace Editor
{
    /// <summary>
    /// Lets a programmer see the current turn order and poke at it by hand while in Play mode —
    /// pick a Unit by name, pick an operation, hit Aceptar. Read-only outside Play mode since
    /// there's no queue to show yet.
    /// </summary>
    [CustomEditor(typeof(TimelineController))]
    public class TimelineControllerEditor : UnityEditor.Editor
    {
        private enum DebugOperation
        {
            Pop, Reinsert, Advance, Delay, MoveToFront, MoveToBack, Swap, InsertAfter, GrantExtraTurn, Remove, Clear
        }

        private string unitNameInput = "";
        private string secondaryUnitNameInput = "";
        private int amountInput = 1;
        private DebugOperation selectedOperation;

        // Keeps the queue list live while the game is running, without extra polling machinery.
        private void OnInspectorUpdate() => Repaint();

        public override void OnInspectorGUI()
        {
            var controller = (TimelineController)target;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Entrá en Play para ver y manipular la cola.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Timeline actual", EditorStyles.boldLabel);
            DrawQueue(controller);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Manipular", EditorStyles.boldLabel);
            DrawControls(controller);
        }

        private void DrawQueue(TimelineController controller)
        {
            var queue = controller.Queue;

            if (queue.Count == 0)
            {
                EditorGUILayout.LabelField("(vacía)");
                return;
            }

            for (int i = 0; i < queue.Count; i++)
            {
                CombatUnit unit = queue[i];
                string label = unit != null ? $"{i}.  {unit.Name}  ·  {unit.Team}" : $"{i}.  <null>";
                EditorGUILayout.LabelField(label);
            }
        }

        private void DrawControls(TimelineController controller)
        {
            unitNameInput = EditorGUILayout.TextField("Unit", unitNameInput);
            selectedOperation = (DebugOperation)EditorGUILayout.EnumPopup("Función", selectedOperation);

            if (selectedOperation is DebugOperation.Swap or DebugOperation.InsertAfter)
            {
                string label = selectedOperation == DebugOperation.Swap ? "Unit B" : "Insertar después de";
                secondaryUnitNameInput = EditorGUILayout.TextField(label, secondaryUnitNameInput);
            }

            if (selectedOperation is DebugOperation.Advance or DebugOperation.Delay)
                amountInput = EditorGUILayout.IntField("N", amountInput);

            if (GUILayout.Button("Aceptar"))
                Apply(controller);
        }

        private void Apply(TimelineController controller)
        {
            switch (selectedOperation)
            {
                case DebugOperation.Clear:
                    controller.Clear();
                    return;
                case DebugOperation.Pop:
                    controller.Pop();
                    return;
            }

            CombatUnit unit = FindUnit(controller, unitNameInput);
            if (unit == null)
            {
                Debug.LogWarning($"[TimelineControllerEditor] No se encontró una Unit llamada '{unitNameInput}' en la cola.");
                return;
            }

            switch (selectedOperation)
            {
                case DebugOperation.Reinsert:
                    controller.Reinsert(unit);
                    break;
                case DebugOperation.Advance:
                    controller.Advance(unit, amountInput);
                    break;
                case DebugOperation.Delay:
                    controller.Delay(unit, amountInput);
                    break;
                case DebugOperation.MoveToFront:
                    controller.MoveToFront(unit);
                    break;
                case DebugOperation.MoveToBack:
                    controller.MoveToBack(unit);
                    break;
                case DebugOperation.GrantExtraTurn:
                    controller.GrantExtraTurn(unit);
                    break;
                case DebugOperation.Remove:
                    controller.Remove(unit);
                    break;
                case DebugOperation.Swap:
                {
                    CombatUnit unitB = FindUnit(controller, secondaryUnitNameInput);
                    if (unitB == null) { Debug.LogWarning($"[TimelineControllerEditor] No se encontró '{secondaryUnitNameInput}'."); return; }
                    controller.Swap(unit, unitB);
                    break;
                }
                case DebugOperation.InsertAfter:
                {
                    CombatUnit after = FindUnit(controller, secondaryUnitNameInput);
                    if (after == null) { Debug.LogWarning($"[TimelineControllerEditor] No se encontró '{secondaryUnitNameInput}'."); return; }
                    controller.InsertAfter(unit, after);
                    break;
                }
            }
        }

        private CombatUnit FindUnit(TimelineController controller, string unitName)
        {
            return controller.Queue.FirstOrDefault(u => u != null && u.Name == unitName);
        }
    }
}