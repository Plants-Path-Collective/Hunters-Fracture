using CombatSystem.UnitSystem;
using UnityEditor;
using UnityEngine;

namespace CombatSystem.UnitSystem.EditorTools
{
    /// <summary>
    /// Inspector personalizado de PartySO. Reemplaza la lista lineal por
    /// defecto por una grilla de retratos, similar a una pantalla de
    /// selección de grupo: cada casillero muestra el retrato y el nombre
    /// del CharacterSheetSO asignado, y acepta arrastrar y soltar assets.
    /// </summary>
    [CustomEditor(typeof(PartySO))]
    public class PartySOEditor : Editor
    {
        private SerializedProperty _members;

        private const float SlotSize = 96f;
        private const float SlotSpacing = 8f;

        private void OnEnable()
        {
            _members = serializedObject.FindProperty("members");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader();
            GUILayout.Space(8);
            DrawToolbar();
            GUILayout.Space(6);
            DrawGrid();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            Rect headerRect = EditorGUILayout.GetControlRect(false, 30);
            EditorGUI.DrawRect(headerRect, JRPGInspectorStyles.HeaderBackground(JRPGInspectorStyles.AllyColor));

            var titleStyle = new GUIStyle(JRPGInspectorStyles.SheetTitle)
            {
                fontSize = 15,
                normal = { textColor = Color.white }
            };
            Rect labelRect = new Rect(headerRect.x + 8, headerRect.y, headerRect.width - 16, headerRect.height);
            EditorGUI.LabelField(labelRect, target.name, titleStyle);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Miembros: {_members.arraySize}", EditorStyles.miniBoldLabel, GUILayout.Width(90));

            if (_members.arraySize > 3)
            {
                EditorGUILayout.HelpBox("Un grupo normalmente admite un máximo de 3 miembros.", MessageType.Warning);
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Agregar espacio", EditorStyles.miniButtonLeft, GUILayout.Width(110)))
            {
                _members.arraySize++;
            }

            using (new EditorGUI.DisabledScope(_members.arraySize == 0))
            {
                if (GUILayout.Button("Quitar espacio", EditorStyles.miniButtonRight, GUILayout.Width(110)))
                {
                    _members.arraySize--;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGrid()
        {
            float inspectorWidth = EditorGUIUtility.currentViewWidth - 20;
            int columns = Mathf.Max(1, Mathf.FloorToInt(inspectorWidth / (SlotSize + SlotSpacing)));

            int count = _members.arraySize;
            int row = 0;

            while (row * columns < count)
            {
                EditorGUILayout.BeginHorizontal();
                for (int col = 0; col < columns; col++)
                {
                    int index = row * columns + col;
                    if (index >= count)
                    {
                        GUILayout.Space(SlotSize + SlotSpacing);
                        continue;
                    }

                    DrawSlot(_members.GetArrayElementAtIndex(index), index);
                    GUILayout.Space(SlotSpacing);
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(SlotSpacing);
                row++;
            }

            if (count == 0)
            {
                EditorGUILayout.HelpBox("Este grupo no tiene espacios de miembros. Usá \"Agregar espacio\" para crear uno.", MessageType.Info);
            }
        }

        private void DrawSlot(SerializedProperty memberProp, int index)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(SlotSize));

            Rect slotRect = GUILayoutUtility.GetRect(SlotSize, SlotSize, GUILayout.Width(SlotSize));
            var sheet = memberProp.objectReferenceValue as CharacterSheetSO;

            GUI.Box(slotRect, GUIContent.none, EditorStyles.helpBox);

            if (sheet != null && sheet.characterPortrait != null && sheet.characterPortrait.texture != null)
            {
                DrawSpritePreview(slotRect, sheet.characterPortrait);
            }
            else if (sheet != null)
            {
                GUI.Label(slotRect, "(sin retrato)", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                GUI.Label(slotRect, "Vacío", EditorStyles.centeredGreyMiniLabel);
            }

            HandleDragAndDrop(slotRect, memberProp);
            HandleClick(slotRect, sheet);

            string label = sheet != null
                ? (string.IsNullOrEmpty(sheet.characterName) ? sheet.name : sheet.characterName)
                : "—";

            var nameStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            GUILayout.Label(label, nameStyle, GUILayout.Width(SlotSize));

            if (sheet != null && GUILayout.Button("Quitar", EditorStyles.miniButton, GUILayout.Width(SlotSize)))
            {
                memberProp.objectReferenceValue = null;
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawSpritePreview(Rect rect, Sprite sprite)
        {
            Rect textureRect = sprite.textureRect;
            Rect uv = new Rect(
                textureRect.x / sprite.texture.width,
                textureRect.y / sprite.texture.height,
                textureRect.width / sprite.texture.width,
                textureRect.height / sprite.texture.height);

            Rect padded = new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4);
            GUI.DrawTextureWithTexCoords(padded, sprite.texture, uv);
        }

        private static void HandleDragAndDrop(Rect rect, SerializedProperty memberProp)
        {
            Event evt = Event.current;
            if (!rect.Contains(evt.mousePosition)) return;

            if (evt.type == EventType.DragUpdated)
            {
                bool valid = DragAndDrop.objectReferences.Length > 0 &&
                             DragAndDrop.objectReferences[0] is CharacterSheetSO;
                DragAndDrop.visualMode = valid ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                if (DragAndDrop.objectReferences.Length > 0 &&
                    DragAndDrop.objectReferences[0] is CharacterSheetSO sheet)
                {
                    DragAndDrop.AcceptDrag();
                    memberProp.objectReferenceValue = sheet;
                    memberProp.serializedObject.ApplyModifiedProperties();
                }
                evt.Use();
            }
        }

        private static void HandleClick(Rect rect, CharacterSheetSO sheet)
        {
            Event evt = Event.current;
            if (evt.type == EventType.MouseDown && evt.button == 0 && rect.Contains(evt.mousePosition))
            {
                if (sheet != null)
                {
                    EditorGUIUtility.PingObject(sheet);
                    if (evt.clickCount == 2)
                    {
                        Selection.activeObject = sheet;
                    }
                }
                evt.Use();
            }
        }
    }
}
