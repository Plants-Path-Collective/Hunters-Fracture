using UnityEditor;
using UnityEngine;

namespace CombatSystem.UnitSystem.EditorTools
{
    /// <summary>
    /// Estilos y utilidades visuales compartidas por los inspectores personalizados
    /// del sistema de combate (CharacterSheetSO, PartySO, SkillSO).
    /// El objetivo es que todos los inspectores luzcan como parte de una misma
    /// "hoja de personaje", con un tono formal y prolijo, sin iconografía informal.
    /// </summary>
    internal static class JRPGInspectorStyles
    {
        // ------- Paleta -------
        public static Color AllyColor => new Color(0.36f, 0.55f, 0.78f);
        public static Color EnemyColor => new Color(0.72f, 0.33f, 0.33f);
        public static Color NeutralColor => new Color(0.5f, 0.5f, 0.5f);

        public static Color SeparatorColor => EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.12f)
            : new Color(0f, 0f, 0f, 0.18f);

        public static Color HeaderBackground(Color accent)
        {
            return EditorGUIUtility.isProSkin
                ? Color.Lerp(accent, Color.black, 0.55f)
                : Color.Lerp(accent, Color.white, 0.55f);
        }

        // ------- Estilos de texto -------
        private static GUIStyle _sheetTitle;
        public static GUIStyle SheetTitle => _sheetTitle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 17,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
        };

        private static GUIStyle _sheetSubtitle;
        public static GUIStyle SheetSubtitle => _sheetSubtitle ??= new GUIStyle(EditorStyles.miniBoldLabel)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(1f, 1f, 1f, 0.85f) }
        };

        private static GUIStyle _sectionHeader;
        public static GUIStyle SectionHeader => _sectionHeader ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12
        };

        private static GUIStyle _statLabel;
        public static GUIStyle StatLabel => _statLabel ??= new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 9
        };

        private static GUIStyle _statBox;
        public static GUIStyle StatBox => _statBox ??= new GUIStyle("HelpBox")
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(2, 2, 4, 4)
        };

        private static GUIStyle _slotBox;
        public static GUIStyle SlotBox => _slotBox ??= new GUIStyle("HelpBox")
        {
            alignment = TextAnchor.MiddleCenter,
            imagePosition = ImagePosition.ImageOnly
        };

        private static GUIStyle _nameField;
        public static GUIStyle NameField => _nameField ??= new GUIStyle(EditorStyles.textField)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            fixedHeight = 22
        };

        // ------- Helpers de dibujo -------
        public static void DrawSeparator(float spaceBefore = 4f, float spaceAfter = 4f, float thickness = 1f)
        {
            GUILayout.Space(spaceBefore);
            Rect rect = EditorGUILayout.GetControlRect(false, thickness);
            EditorGUI.DrawRect(rect, SeparatorColor);
            GUILayout.Space(spaceAfter);
        }

        /// <summary>
        /// Dibuja un "casillero" de estadística, editable, al estilo de una
        /// ficha de personaje (nombre corto arriba, valor grande abajo).
        /// </summary>
        public static void DrawStatBox(SerializedProperty prop, string label, float width = 72f, float height = 46f)
        {
            GUILayout.BeginVertical(StatBox, GUILayout.Width(width), GUILayout.Height(height));
            GUILayout.Label(label.ToUpperInvariant(), StatLabel);

            var fieldStyle = new GUIStyle(EditorStyles.numberField)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 13
            };

            prop.floatValue = EditorGUILayout.FloatField(prop.floatValue, fieldStyle, GUILayout.Width(width - 8));
            GUILayout.EndVertical();
        }

        public static void DrawSectionHeader(string title)
        {
            GUILayout.Space(6);
            EditorGUILayout.LabelField(title, SectionHeader);
            DrawSeparator(2, 4, 1.5f);
        }
    }
}
