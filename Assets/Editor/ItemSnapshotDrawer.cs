using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Core.CombatSystem.ItemSystem;

namespace Core.EditorTools
{
    /// <summary>
    /// Draws an ItemSnapshot as a reorderable table: [handle | ID | Item | Qty]. The ItemSO
    /// picked is stored as its itemID; existing IDs are resolved back to their ItemSO through
    /// the project's ItemCatalog asset. Rows with a repeated item are tinted red (first wins).
    /// </summary>
    [CustomPropertyDrawer(typeof(ItemSnapshot))]
    public class ItemSnapshotDrawer : PropertyDrawer
    {
        private const int UnassignedKey = -1;
        private const float Indent = 15f;
        private const float IdWidth = 34f;
        private const float QuantityWidth = 48f;
        private const float Spacing = 2f;
        private const float RowPadding = 2f;

        // Space ReorderableList reserves for the drag handle (left) and its inner margin (right).
        // Only used to align the column header with the rows: tweak these if it looks off.
        private const float HandleWidth = 20f;
        private const float HeaderRightPadding = 6f;

        private static readonly GUIContent IdHeader = new("ID",
            "itemID of the item, as stored in the snapshot and used as the key in the ItemCatalog. Read-only: it follows the item you pick.");
        private static readonly GUIContent ItemHeader = new("Item",
            "The ItemSO held by this unit. Drag one here or use the picker; only items listed in the ItemCatalog can be resolved.");
        private static readonly GUIContent QuantityHeader = new("Qty",
            "How many copies of the item the unit holds (each copy is a stack).");

        private static GUIStyle centeredMini;
        private static GUIStyle centeredMiniBold;
        private static ItemCatalog cachedCatalog;

        // One ReorderableList per drawn property; rebuilt when the SerializedObject changes.
        private readonly Dictionary<string, (SerializedObject owner, ReorderableList list)> lists = new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
                return line;

            return line + Spacing + GetList(property).GetHeight();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            centeredMini ??= new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            centeredMiniBold ??= new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter };

            SerializedProperty entries = property.FindPropertyRelative("entries");
            float line = EditorGUIUtility.singleLineHeight;

            var foldout = new Rect(position.x, position.y, position.width, line);
            property.isExpanded = EditorGUI.Foldout(foldout, property.isExpanded,
                new GUIContent($"{label.text} ({entries.arraySize})", "Item quantities held, keyed by itemID."), true);
            if (!property.isExpanded) return;

            ReorderableList list = GetList(property);
            var listRect = new Rect(position.x + Indent, position.y + line + Spacing,
                position.width - Indent, list.GetHeight());
            list.DoList(listRect);
        }

        // ── List setup ────────────────────────────────────────────────────────

        private ReorderableList GetList(SerializedProperty property)
        {
            string cacheKey = property.serializedObject.targetObject.GetInstanceID() + "/" + property.propertyPath;

            if (lists.TryGetValue(cacheKey, out var cached) && cached.owner == property.serializedObject)
                return cached.list;

            SerializedProperty entries = property.FindPropertyRelative("entries");
            var list = new ReorderableList(property.serializedObject, entries,
                draggable: true, displayHeader: true, displayAddButton: true, displayRemoveButton: true)
            {
                elementHeight = EditorGUIUtility.singleLineHeight + RowPadding * 2f
            };

            list.drawHeaderCallback = DrawColumnHeader;
            list.drawElementCallback = (rect, index, isActive, isFocused) => DrawRow(rect, entries, index);
            list.onAddCallback = l =>
            {
                int index = l.serializedProperty.arraySize;
                l.serializedProperty.arraySize++;
                l.index = index;

                SerializedProperty added = l.serializedProperty.GetArrayElementAtIndex(index);
                added.FindPropertyRelative("key").intValue = UnassignedKey;
                added.FindPropertyRelative("value").intValue = 1;
            };

            lists[cacheKey] = (property.serializedObject, list);
            return list;
        }

        // ── Drawing ───────────────────────────────────────────────────────────

        private static void GetColumns(Rect row, out Rect id, out Rect item, out Rect quantity)
        {
            float itemWidth = row.width - IdWidth - QuantityWidth - Spacing * 2;

            id = new Rect(row.x, row.y, IdWidth, row.height);
            item = new Rect(id.xMax + Spacing, row.y, itemWidth, row.height);
            quantity = new Rect(item.xMax + Spacing, row.y, QuantityWidth, row.height);
        }

        private static void DrawColumnHeader(Rect header)
        {
            var inner = new Rect(header.x + HandleWidth, header.y,
                header.width - HandleWidth - HeaderRightPadding, header.height);
            GetColumns(inner, out Rect id, out Rect item, out Rect quantity);

            EditorGUI.LabelField(id, IdHeader, centeredMiniBold);
            EditorGUI.LabelField(item, ItemHeader, EditorStyles.miniBoldLabel);
            EditorGUI.LabelField(quantity, QuantityHeader, centeredMiniBold);
        }

        private static void DrawRow(Rect rect, SerializedProperty entries, int index)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            SerializedProperty key = entry.FindPropertyRelative("key");
            SerializedProperty value = entry.FindPropertyRelative("value");

            var row = new Rect(rect.x, rect.y + RowPadding, rect.width, EditorGUIUtility.singleLineHeight);
            GetColumns(row, out Rect idRect, out Rect itemRect, out Rect quantityRect);

            Color previousColor = GUI.color;
            if (IsDuplicate(entries, index))
                GUI.color = new Color(1f, 0.6f, 0.6f);

            // ID column (read-only)
            bool hasKey = key.intValue != UnassignedKey;
            EditorGUI.LabelField(idRect,
                new GUIContent(hasKey ? key.intValue.ToString() : "—",
                    hasKey ? "itemID stored for this row." : "No item assigned yet."),
                centeredMini);

            // Item column
            ItemSO current = FindItem(GetCatalog(), key.intValue);

            if (current != null || !hasKey)
            {
                EditorGUI.BeginChangeCheck();
                var picked = (ItemSO)EditorGUI.ObjectField(itemRect, current, typeof(ItemSO), false);
                if (EditorGUI.EndChangeCheck())
                    key.intValue = picked != null ? picked.itemID : UnassignedKey;
            }
            else
            {
                EditorGUI.LabelField(itemRect,
                    new GUIContent("(not in ItemCatalog)", $"itemID {key.intValue} is not registered in the ItemCatalog."));
            }

            // Quantity column
            EditorGUI.BeginChangeCheck();
            int quantity = EditorGUI.IntField(quantityRect, value.intValue);
            if (EditorGUI.EndChangeCheck())
                value.intValue = Mathf.Max(0, quantity);

            GUI.color = previousColor;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static bool IsDuplicate(SerializedProperty entries, int index)
        {
            int keyValue = entries.GetArrayElementAtIndex(index).FindPropertyRelative("key").intValue;

            for (int i = 0; i < index; i++)
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("key").intValue == keyValue)
                    return true;

            return false;
        }

        private static ItemCatalog GetCatalog()
        {
            if (cachedCatalog != null) return cachedCatalog;

            string[] guids = AssetDatabase.FindAssets("t:ItemCatalog");
            if (guids.Length == 0) return null;

            cachedCatalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));
            return cachedCatalog;
        }

        private static ItemSO FindItem(ItemCatalog catalog, int itemID)
        {
            if (catalog == null) return null;

            foreach (ItemSO item in catalog.AllItems)
                if (item != null && item.itemID == itemID)
                    return item;

            return null;
        }
    }
}