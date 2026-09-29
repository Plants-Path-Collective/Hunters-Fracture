using UnityEditor;
using UnityEngine;
using Core.CombatSystem.Units;

namespace Core.EditorTools
{
    /// <summary>
    /// Draws a PartyMemberData row: definition, a "Start at Full HP/SP" toggle standing in for
    /// the -1 sentinel (so the raw -1 is never typed by hand), and the inventory snapshot.
    /// </summary>
    [CustomPropertyDrawer(typeof(PartyMemberData))]
    public class PartyMemberDataDrawer : PropertyDrawer
    {
        private const float Spacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            bool full = property.FindPropertyRelative("currentHP").intValue < 0;

            float height = (line + Spacing) * 2f; // definition + toggle
            if (!full) height += (line + Spacing) * 2f; // HP + SP fields

            SerializedProperty snapshot = property.FindPropertyRelative("inventorySnapshot");
            height += EditorGUI.GetPropertyHeight(snapshot, GUIContent.none, true) + Spacing;

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty definition = property.FindPropertyRelative("definition");
            SerializedProperty currentHP = property.FindPropertyRelative("currentHP");
            SerializedProperty currentSP = property.FindPropertyRelative("currentSP");
            SerializedProperty snapshot = property.FindPropertyRelative("inventorySnapshot");

            float line = EditorGUIUtility.singleLineHeight;
            float y = position.y;

            EditorGUI.PropertyField(new Rect(position.x, y, position.width, line), definition);
            y += line + Spacing;

            bool full = currentHP.intValue < 0;
            EditorGUI.BeginChangeCheck();
            bool newFull = EditorGUI.ToggleLeft(new Rect(position.x, y, position.width, line),
                new GUIContent("Start at Full HP/SP",
                    "On: enters combat at the recalculated max. Off: enters at the values below (0 = starts dead)."),
                full);
            if (EditorGUI.EndChangeCheck())
            {
                if (newFull)
                {
                    currentHP.intValue = -1;
                    currentSP.intValue = -1;
                }
                else if (full)
                {
                    // Was full, just unchecked: seed it from the definition instead of a raw 0.
                    var def = definition.objectReferenceValue as UnitDefinitionSO;
                    currentHP.intValue = def != null ? def.maxHP : 0;
                    currentSP.intValue = def != null ? def.maxSP : 0;
                }
                full = newFull;
            }
            y += line + Spacing;

            if (!full)
            {
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, line), currentHP, new GUIContent("Current HP"));
                y += line + Spacing;
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, line), currentSP, new GUIContent("Current SP"));
                y += line + Spacing;
            }

            float snapshotHeight = EditorGUI.GetPropertyHeight(snapshot, GUIContent.none, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, snapshotHeight),
                snapshot, new GUIContent("Inventory Snapshot"), true);
        }
    }
}