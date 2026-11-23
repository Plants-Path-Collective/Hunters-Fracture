using UnityEditor;
using UnityEngine;

namespace Core.QuestsSystem.Editor
{
    [CustomPropertyDrawer(typeof(ShowIfRewardAttribute))]
    public class ShowIfRewardDrawer : PropertyDrawer
    {
        private bool IsVisible(SerializedProperty property)
        {
            var attr = (ShowIfRewardAttribute)attribute;
            SerializedProperty enumProp = FindSibling(property, attr.EnumField);

            if (enumProp == null || enumProp.propertyType != SerializedPropertyType.Enum)
                return true; // wrong field name: show it so the mistake is obvious

            int index = enumProp.enumValueIndex;
            if (index < 0) return true; // different values selected (multi-object editing)

            return enumProp.enumNames[index] == attr.Value.ToString();
        }

        // Looks for a field next to 'property' (same parent), so it works at the root
        // of an asset and inside lists/arrays/nested classes.
        private static SerializedProperty FindSibling(SerializedProperty property, string siblingName)
        {
            string path = property.propertyPath;
            int lastDot = path.LastIndexOf('.');

            // No dot: the field lives at the root of the object.
            if (lastDot < 0) return property.serializedObject.FindProperty(siblingName);

            string siblingPath = path.Substring(0, lastDot + 1) + siblingName;
            return property.serializedObject.FindProperty(siblingPath);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Negative spacing cancels the gap Unity adds between fields, so hidden fields leave no hole.
            return IsVisible(property)
                ? EditorGUI.GetPropertyHeight(property, label, true)
                : -EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (IsVisible(property))
                EditorGUI.PropertyField(position, property, label, true);
        }
    }
}