using CombatSystem.UnitSystem;
using UnityEditor;
using UnityEngine;

namespace CombatSystem.UnitSystem.EditorTools
{
    /// <summary>
    /// Inspector personalizado de SkillSO. Presenta la habilidad como una
    /// "carta": icono a la izquierda, información general a la derecha,
    /// costos como casilleros de estadística y el resto en secciones
    /// plegables, coherente con CharacterSheetSOEditor.
    /// </summary>
    [CustomEditor(typeof(SkillSO))]
    [CanEditMultipleObjects]
    public class SkillSOEditor : Editor
    {
        private SerializedProperty _skillName;
        private SerializedProperty _description;
        private SerializedProperty _icon;
        private SerializedProperty _animationClip;
        private SerializedProperty _animationDuration;

        private SerializedProperty _skillType;
        private SerializedProperty _targetType;

        private SerializedProperty _spCost;
        private SerializedProperty _hpCost;
        private SerializedProperty _usageLimit;

        private SerializedProperty _effects;

        private SerializedProperty _basePower;
        private SerializedProperty _accuracy;
        private SerializedProperty _damageType;

        private SerializedProperty _speedModifier;
        private SerializedProperty _isUltimate;
        private SerializedProperty _isPassive;
        private SerializedProperty _cooldown;

        private const float IconSize = 80f;

        private void OnEnable()
        {
            _skillName = serializedObject.FindProperty("skillName");
            _description = serializedObject.FindProperty("description");
            _icon = serializedObject.FindProperty("icon");
            _animationClip = serializedObject.FindProperty("animationClip");
            _animationDuration = serializedObject.FindProperty("animationDuration");

            _skillType = serializedObject.FindProperty("skillType");
            _targetType = serializedObject.FindProperty("targetType");

            _spCost = serializedObject.FindProperty("spCost");
            _hpCost = serializedObject.FindProperty("hpCost");
            _usageLimit = serializedObject.FindProperty("usageLimit");

            _effects = serializedObject.FindProperty("effects");

            _basePower = serializedObject.FindProperty("basePower");
            _accuracy = serializedObject.FindProperty("accuracy");
            _damageType = serializedObject.FindProperty("damageType");

            _speedModifier = serializedObject.FindProperty("speedModifier");
            _isUltimate = serializedObject.FindProperty("isUltimate");
            _isPassive = serializedObject.FindProperty("isPassive");
            _cooldown = serializedObject.FindProperty("cooldown");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader();
            GUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();
            DrawIconColumn();
            GUILayout.Space(10);
            DrawGeneralInfoColumn();
            EditorGUILayout.EndHorizontal();

            JRPGInspectorStyles.DrawSectionHeader("Descripción");
            EditorGUILayout.PropertyField(_description, GUIContent.none, GUILayout.Height(50));

            JRPGInspectorStyles.DrawSectionHeader("Costos");
            EditorGUILayout.BeginHorizontal();
            JRPGInspectorStyles.DrawStatBox(_spCost, "Costo SP");
            JRPGInspectorStyles.DrawStatBox(_hpCost, "Costo HP");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(_usageLimit, new GUIContent("Usos por combate (-1 = ilimitado)"));

            JRPGInspectorStyles.DrawSectionHeader("Modificadores de Combate");
            EditorGUILayout.BeginHorizontal();
            JRPGInspectorStyles.DrawStatBox(_basePower, "Poder Base");
            JRPGInspectorStyles.DrawStatBox(_accuracy, "Precisión");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(_damageType, new GUIContent("Tipo de Daño"));
            EditorGUILayout.PropertyField(_speedModifier, new GUIContent("Modificador de Velocidad"));
            EditorGUILayout.PropertyField(_cooldown, new GUIContent("Cooldown (segundos ATB)"));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_isUltimate, new GUIContent("Es Ultimate"));
            EditorGUILayout.PropertyField(_isPassive, new GUIContent("Es Pasiva"));
            EditorGUILayout.EndHorizontal();

            JRPGInspectorStyles.DrawSectionHeader("Efectos");
            EditorGUILayout.PropertyField(_effects, true);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            Color accent = _isUltimate.boolValue
                ? new Color(0.68f, 0.53f, 0.16f) // dorado, para ultimates
                : JRPGInspectorStyles.NeutralColor;

            Rect headerRect = EditorGUILayout.GetControlRect(false, 30);
            EditorGUI.DrawRect(headerRect, JRPGInspectorStyles.HeaderBackground(accent));

            string displayName = string.IsNullOrEmpty(_skillName.stringValue) ? target.name : _skillName.stringValue;
            var titleStyle = new GUIStyle(JRPGInspectorStyles.SheetTitle)
            {
                fontSize = 15,
                normal = { textColor = Color.white }
            };
            Rect labelRect = new Rect(headerRect.x + 8, headerRect.y, headerRect.width - 100, headerRect.height);
            EditorGUI.LabelField(labelRect, displayName, titleStyle);

            if (_isUltimate.boolValue)
            {
                Rect tagRect = new Rect(headerRect.xMax - 80, headerRect.y + 6, 72, 18);
                var tagStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = Color.white }
                };
                EditorGUI.LabelField(tagRect, "ULTIMATE", tagStyle);
            }
        }

        private void DrawIconColumn()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(IconSize));

            Rect iconRect = GUILayoutUtility.GetRect(IconSize, IconSize, GUILayout.Width(IconSize));
            GUI.Box(iconRect, GUIContent.none, EditorStyles.helpBox);

            var sprite = _icon.objectReferenceValue as Sprite;
            if (sprite != null && sprite.texture != null)
            {
                Rect textureRect = sprite.textureRect;
                Rect uv = new Rect(
                    textureRect.x / sprite.texture.width,
                    textureRect.y / sprite.texture.height,
                    textureRect.width / sprite.texture.width,
                    textureRect.height / sprite.texture.height);
                Rect padded = new Rect(iconRect.x + 2, iconRect.y + 2, iconRect.width - 4, iconRect.height - 4);
                GUI.DrawTextureWithTexCoords(padded, sprite.texture, uv);
            }
            else
            {
                GUI.Label(iconRect, "Sin icono", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.PropertyField(_icon, GUIContent.none);
            EditorGUILayout.EndVertical();
        }

        private void DrawGeneralInfoColumn()
        {
            EditorGUILayout.BeginVertical();

            _skillName.stringValue = EditorGUILayout.TextField("Nombre", _skillName.stringValue);
            EditorGUILayout.PropertyField(_skillType, new GUIContent("Tipo de Skill"));
            EditorGUILayout.PropertyField(_targetType, new GUIContent("Objetivo"));
            EditorGUILayout.PropertyField(_animationClip, new GUIContent("Animación"));
            EditorGUILayout.PropertyField(_animationDuration, new GUIContent("Duración (afecta ATB)"));

            EditorGUILayout.EndVertical();
        }
    }
}
