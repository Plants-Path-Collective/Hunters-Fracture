using CombatSystem.UnitSystem;
using UnityEditor;
using UnityEngine;

namespace CombatSystem.UnitSystem.EditorTools
{
    /// <summary>
    /// Inspector personalizado de CharacterSheetSO.
    /// Presenta la información como una ficha de personaje: retrato a la
    /// izquierda, datos generales y estadísticas a la derecha, y el resto
    /// del contenido (skills, prefab, animaciones, descripción) organizado
    /// en secciones plegables debajo.
    /// </summary>
    [CustomEditor(typeof(CharacterSheetSO))]
    [CanEditMultipleObjects]
    public class CharacterSheetSOEditor : Editor
    {
        private SerializedProperty _unitType;
        private SerializedProperty _behavior;
        private SerializedProperty _damageType;
        private SerializedProperty _skills;
        private SerializedProperty _combatPrefab;

        private SerializedProperty _hp;
        private SerializedProperty _sp;
        private SerializedProperty _speed;
        private SerializedProperty _strength;
        private SerializedProperty _magicPower;
        private SerializedProperty _evasion;
        private SerializedProperty _accuracy;
        private SerializedProperty _physicalDefense;
        private SerializedProperty _magicalDefense;

        private SerializedProperty _portrait;
        private SerializedProperty _characterName;
        private SerializedProperty _characterDescription;

        private SerializedProperty _owIdle;
        private SerializedProperty _owWalk;
        private SerializedProperty _owInteract;
        private SerializedProperty _cbIdle;
        private SerializedProperty _cbRun;
        private SerializedProperty _cbBasicAttack;
        private SerializedProperty _cbSkill1;
        private SerializedProperty _cbSkill2;
        private SerializedProperty _cbUltimate;

        private const float PortraitSize = 128f;

        private void OnEnable()
        {
            _unitType = serializedObject.FindProperty("unitType");
            _behavior = serializedObject.FindProperty("behavior");
            _damageType = serializedObject.FindProperty("damageType");
            _skills = serializedObject.FindProperty("skills");
            _combatPrefab = serializedObject.FindProperty("combatPrefab");

            _hp = serializedObject.FindProperty("HP");
            _sp = serializedObject.FindProperty("SP");
            _speed = serializedObject.FindProperty("speed");
            _strength = serializedObject.FindProperty("strenght"); // nombre original conservado
            _magicPower = serializedObject.FindProperty("magicPower");
            _evasion = serializedObject.FindProperty("evasion");
            _accuracy = serializedObject.FindProperty("accuracy");
            _physicalDefense = serializedObject.FindProperty("physicalDefense");
            _magicalDefense = serializedObject.FindProperty("magicalDefense");

            _portrait = serializedObject.FindProperty("characterPortrait");
            _characterName = serializedObject.FindProperty("characterName");
            _characterDescription = serializedObject.FindProperty("characterDescription");

            _owIdle = serializedObject.FindProperty("ow_idleAnimation");
            _owWalk = serializedObject.FindProperty("ow_walkAnimation");
            _owInteract = serializedObject.FindProperty("ow_interactAnimation");
            _cbIdle = serializedObject.FindProperty("cb_idleAnimation");
            _cbRun = serializedObject.FindProperty("cb_runAnimation");
            _cbBasicAttack = serializedObject.FindProperty("cb_basicAttackAnimation");
            _cbSkill1 = serializedObject.FindProperty("cb_skill1Animation");
            _cbSkill2 = serializedObject.FindProperty("cb_skill2Animation");
            _cbUltimate = serializedObject.FindProperty("cb_ultimateAnimation");
        }

        private bool IsEnemy => _unitType.enumDisplayNames[_unitType.enumValueIndex]
            .IndexOf("Enemy", System.StringComparison.OrdinalIgnoreCase) >= 0;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader();
            GUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();
            DrawPortraitColumn();
            GUILayout.Space(10);
            DrawGeneralInfoColumn();
            EditorGUILayout.EndHorizontal();

            JRPGInspectorStyles.DrawSectionHeader("Estadísticas");
            DrawStatGrid();

            JRPGInspectorStyles.DrawSectionHeader("Habilidades");
            EditorGUILayout.PropertyField(_skills, new GUIContent("Skills"), true);

            JRPGInspectorStyles.DrawSectionHeader("Descripción");
            EditorGUILayout.PropertyField(_characterDescription, GUIContent.none, GUILayout.Height(60));

            GUILayout.Space(6);
            DrawFoldoutSection("Prefab de Combate", "cs_prefab", () =>
            {
                EditorGUILayout.PropertyField(_combatPrefab);
            });

            DrawFoldoutSection("Animaciones — Overworld", "cs_ow_anim", () =>
            {
                EditorGUILayout.PropertyField(_owIdle);
                EditorGUILayout.PropertyField(_owWalk);
                EditorGUILayout.PropertyField(_owInteract);
            });

            DrawFoldoutSection("Animaciones — Combate", "cs_cb_anim", () =>
            {
                EditorGUILayout.PropertyField(_cbIdle);
                EditorGUILayout.PropertyField(_cbRun);
                EditorGUILayout.PropertyField(_cbBasicAttack);
                EditorGUILayout.PropertyField(_cbSkill1);
                EditorGUILayout.PropertyField(_cbSkill2);
                EditorGUILayout.PropertyField(_cbUltimate);
            });

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            Color accent = IsEnemy ? JRPGInspectorStyles.EnemyColor : JRPGInspectorStyles.AllyColor;
            Rect headerRect = EditorGUILayout.GetControlRect(false, 34);
            EditorGUI.DrawRect(headerRect, JRPGInspectorStyles.HeaderBackground(accent));

            Rect nameRect = new Rect(headerRect.x + 8, headerRect.y + 3, headerRect.width - 140, 20);
            Rect typeRect = new Rect(headerRect.xMax - 130, headerRect.y + 8, 122, 18);

            string displayName = string.IsNullOrEmpty(_characterName.stringValue)
                ? target.name
                : _characterName.stringValue;

            var titleStyle = new GUIStyle(JRPGInspectorStyles.SheetTitle) { normal = { textColor = Color.white } };
            EditorGUI.LabelField(nameRect, displayName, titleStyle);

            var subtitleStyle = new GUIStyle(JRPGInspectorStyles.SheetSubtitle) { alignment = TextAnchor.MiddleRight };
            EditorGUI.LabelField(typeRect, _unitType.enumDisplayNames[_unitType.enumValueIndex], subtitleStyle);
        }

        private void DrawPortraitColumn()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(PortraitSize));

            Rect portraitRect = GUILayoutUtility.GetRect(PortraitSize, PortraitSize, GUILayout.Width(PortraitSize));
            GUI.Box(portraitRect, GUIContent.none, EditorStyles.helpBox);

            var sprite = _portrait.objectReferenceValue as Sprite;
            if (sprite != null && sprite.texture != null)
            {
                DrawSpritePreview(portraitRect, sprite);
            }
            else
            {
                var placeholderStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { wordWrap = true };
                GUI.Label(portraitRect, "Sin retrato asignado", placeholderStyle);
            }

            EditorGUILayout.PropertyField(_portrait, GUIContent.none);
            EditorGUILayout.PropertyField(_damageType, new GUIContent("Tipo de Daño"));

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

        private void DrawGeneralInfoColumn()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("Nombre", EditorStyles.miniBoldLabel);
            _characterName.stringValue = EditorGUILayout.TextField(_characterName.stringValue, JRPGInspectorStyles.NameField);

            GUILayout.Space(4);
            EditorGUILayout.PropertyField(_unitType, new GUIContent("Tipo de Unidad"));

            if (IsEnemy)
            {
                EditorGUILayout.PropertyField(_behavior, new GUIContent("Comportamiento (IA)"));
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.LabelField("Comportamiento", "Solo aplica a unidades enemigas");
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        private void DrawStatGrid()
        {
            EditorGUILayout.BeginHorizontal();
            JRPGInspectorStyles.DrawStatBox(_hp, "HP");
            JRPGInspectorStyles.DrawStatBox(_sp, "SP");
            JRPGInspectorStyles.DrawStatBox(_speed, "Velocidad");
            JRPGInspectorStyles.DrawStatBox(_strength, "Fuerza");
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            JRPGInspectorStyles.DrawStatBox(_magicPower, "Poder Mágico");
            JRPGInspectorStyles.DrawStatBox(_evasion, "Evasión");
            JRPGInspectorStyles.DrawStatBox(_accuracy, "Precisión");
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            JRPGInspectorStyles.DrawStatBox(_physicalDefense, "Def. Física");
            JRPGInspectorStyles.DrawStatBox(_magicalDefense, "Def. Mágica");
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFoldoutSection(string title, string sessionKey, System.Action drawContent)
        {
            string key = $"{sessionKey}_{target.GetInstanceID()}";
            bool expanded = SessionState.GetBool(key, false);

            GUILayout.Space(2);
            bool newExpanded = EditorGUILayout.Foldout(expanded, title, true, EditorStyles.foldoutHeader);
            if (newExpanded != expanded)
            {
                SessionState.SetBool(key, newExpanded);
            }

            if (newExpanded)
            {
                EditorGUI.indentLevel++;
                drawContent.Invoke();
                EditorGUI.indentLevel--;
            }
        }
    }
}
