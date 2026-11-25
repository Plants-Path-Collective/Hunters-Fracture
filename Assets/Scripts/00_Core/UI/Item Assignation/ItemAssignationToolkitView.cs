using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Core.CombatSystem.ItemSystem;
using Core.CombatSystem.Units;

namespace Core.UI
{
    /// <summary>
    /// PLACEHOLDER view built with UI Toolkit. The whole tree is created in code (no UXML/USS) so it works
    /// as soon as it is dropped on an object with a UIDocument that has a Panel Settings asset.
    /// Texts are hardcoded and not localized yet.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ItemAssignationToolkitView : ItemAssignationView
    {
        private sealed class MemberRow
        {
            public VisualElement root;
            public VisualElement portrait;
            public Label nameLabel;
            public VisualElement items;
        }

        private static readonly Color Dim = new(0f, 0f, 0f, 0.7f);
        private static readonly Color Card = new(0.09f, 0.10f, 0.13f, 1f);
        private static readonly Color Slot = new(0.15f, 0.17f, 0.21f, 1f);
        private static readonly Color Accent = new(1f, 0.78f, 0.25f, 1f);
        private static readonly Color Muted = new(0.65f, 0.68f, 0.74f, 1f);

        private UIDocument document;
        private VisualElement overlay;
        private VisualElement unassignedList;
        private VisualElement holdBar;
        private VisualElement holdFill;
        private Label descName;
        private Label descText;
        private VisualElement pillBar;
        private readonly MemberRow[] rows = new MemberRow[AllyParty.MaxMembers];
        private bool visible;

        private void Awake() => document = GetComponent<UIDocument>();

        // ── ItemAssignationView ───────────────────────────────────────────────

        public override void Show()
        {
            visible = true;
            if (EnsureBuilt()) overlay.style.display = DisplayStyle.Flex;
        }

        public override void Hide()
        {
            visible = false;
            if (EnsureBuilt()) overlay.style.display = DisplayStyle.None;
        }

        public override void Render(ItemAssignmentSession session)
        {
            if (!EnsureBuilt()) return;
            if (visible) overlay.style.display = DisplayStyle.Flex;

            RenderUnassigned(session);

            for (int slot = 0; slot < rows.Length; slot++)
                RenderMember(session, slot);

            ItemSO selected = session.Selected;
            descName.text = selected != null ? selected.localizedName.GetLocalizedString() : string.Empty;
            descText.text = selected != null ? selected.localizedDescription.GetLocalizedString() : string.Empty;
        }

        public override void SetHints(IReadOnlyList<InputHint> hints)
        {
            if (!EnsureBuilt()) return;

            pillBar.Clear();
            foreach (InputHint hint in hints)
                pillBar.Add(MakePill(hint));
        }

        public override void SetHoldProgress(float normalized)
        {
            if (!EnsureBuilt()) return;

            holdBar.style.display = normalized > 0f ? DisplayStyle.Flex : DisplayStyle.None;
            holdFill.style.width = Length.Percent(Mathf.Clamp01(normalized) * 100f);
        }

        // ── Rendering ─────────────────────────────────────────────────────────

        private void RenderUnassigned(ItemAssignmentSession session)
        {
            unassignedList.Clear();

            for (int i = 0; i < session.Unassigned.Count; i++)
            {
                ItemSO item = session.Unassigned[i];
                bool selected = i == session.SelectedIndex;

                var entry = new VisualElement();
                entry.style.flexDirection = FlexDirection.Row;
                entry.style.alignItems = Align.Center;
                entry.style.backgroundColor = Slot;
                entry.style.marginBottom = 4;
                SetPadding(entry, 4);
                SetBorder(entry, 2, selected ? Accent : Color.clear, 4);

                entry.Add(MakeIcon(item, 1, 36));

                Label name = MakeLabel(item.localizedName.GetLocalizedString(), 14, selected ? Accent : Color.white);
                name.style.marginLeft = 8;
                entry.Add(name);

                unassignedList.Add(entry);
            }
        }

        private void RenderMember(ItemAssignmentSession session, int slot)
        {
            MemberRow row = rows[slot];
            AssignTarget target = slot < session.SlotCount ? session.GetTarget(slot) : null;

            row.root.style.display = target == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (target == null) return;

            row.nameLabel.text = target.Name;
            SetSprite(row.portrait, target.Portrait);

            row.items.Clear();
            foreach ((ItemSO item, int quantity) in session.GetStacks(slot))
            {
                if (item != null && quantity > 0) row.items.Add(MakeIcon(item, quantity, 36));
            }
        }

        // ── Build ─────────────────────────────────────────────────────────────

        // The UIDocument can recreate its root when it is toggled, so rebuild if the tree is gone.
        private bool EnsureBuilt()
        {
            if (document == null) document = GetComponent<UIDocument>();

            VisualElement root = document != null ? document.rootVisualElement : null;
            if (root == null) return false;
            if (overlay != null && overlay.parent == root) return true;

            Build(root);
            return true;
        }

        private void Build(VisualElement root)
        {
            overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.top = 0;
            overlay.style.right = 0;
            overlay.style.bottom = 0;
            overlay.style.alignItems = Align.Center;
            overlay.style.justifyContent = Justify.Center;
            overlay.style.backgroundColor = Dim;
            overlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            root.Add(overlay);

            // Column: input hint pills above, main card below.
            var column = new VisualElement();
            column.style.width = Length.Percent(80);
            column.style.maxWidth = 1000;
            overlay.Add(column);

            pillBar = new VisualElement();
            pillBar.style.flexDirection = FlexDirection.Row;
            pillBar.style.flexWrap = Wrap.Wrap;
            pillBar.style.justifyContent = Justify.Center;
            pillBar.style.marginBottom = 10;
            column.Add(pillBar);

            var card = new VisualElement();
            card.style.backgroundColor = Card;
            SetPadding(card, 20);
            SetBorder(card, 2, Muted, 8);
            column.Add(card);

            card.Add(MakeLabel("ASSIGN ITEMS", 22, Accent));

            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.marginTop = 12;
            card.Add(body);

            // Left: one row per party slot.
            var left = new VisualElement();
            left.style.flexGrow = 1;
            left.Add(MakeLabel("Party", 14, Muted));
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = BuildMemberRow();
                left.Add(rows[i].root);
            }
            body.Add(left);

            // Right: unassigned items.
            var right = new VisualElement();
            right.style.width = Length.Percent(34);
            right.style.marginLeft = 16;
            right.Add(MakeLabel("Unassigned", 14, Muted));
            unassignedList = new VisualElement();
            unassignedList.style.marginTop = 6;
            right.Add(unassignedList);
            body.Add(right);

            // Description.
            descName = MakeLabel(string.Empty, 18, Color.white);
            descName.style.marginTop = 16;
            card.Add(descName);

            descText = MakeLabel(string.Empty, 14, Muted);
            descText.style.whiteSpace = WhiteSpace.Normal;
            descText.style.minHeight = 40;
            card.Add(descText);

            // Hold-to-confirm bar.
            holdBar = new VisualElement();
            holdBar.style.height = 8;
            holdBar.style.marginTop = 8;
            holdBar.style.backgroundColor = Slot;
            holdBar.style.display = DisplayStyle.None;

            holdFill = new VisualElement();
            holdFill.style.height = Length.Percent(100);
            holdFill.style.width = Length.Percent(0);
            holdFill.style.backgroundColor = Accent;
            holdBar.Add(holdFill);
            card.Add(holdBar);
        }

        private MemberRow BuildMemberRow()
        {
            var row = new MemberRow();

            row.root = new VisualElement();
            row.root.style.flexDirection = FlexDirection.Row;
            row.root.style.alignItems = Align.Center;
            row.root.style.backgroundColor = Slot;
            row.root.style.marginTop = 6;
            SetPadding(row.root, 6);
            SetBorder(row.root, 1, Muted, 4);

            row.portrait = new VisualElement();
            row.portrait.style.width = 64;
            row.portrait.style.height = 64;
            row.portrait.style.marginRight = 10;
            row.root.Add(row.portrait);

            var info = new VisualElement();
            info.style.flexGrow = 1;
            row.nameLabel = MakeLabel(string.Empty, 16, Color.white);
            info.Add(row.nameLabel);

            row.items = new VisualElement();
            row.items.style.flexDirection = FlexDirection.Row;
            row.items.style.flexWrap = Wrap.Wrap;
            row.items.style.marginTop = 4;
            info.Add(row.items);

            row.root.Add(info);
            return row;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        // Rounded pill: key badge + action label. Dimmed when the action does nothing right now.
        private static VisualElement MakePill(InputHint hint)
        {
            var pill = new VisualElement();
            pill.style.flexDirection = FlexDirection.Row;
            pill.style.alignItems = Align.Center;
            pill.style.backgroundColor = Slot;
            pill.style.marginLeft = 4;
            pill.style.marginRight = 4;
            pill.style.marginBottom = 4;
            pill.style.paddingLeft = 4;
            pill.style.paddingRight = 12;
            pill.style.paddingTop = 4;
            pill.style.paddingBottom = 4;
            pill.style.opacity = hint.enabled ? 1f : 0.35f;
            SetBorder(pill, 1, Muted, 16);

            Label key = MakeLabel(hint.key, 12, Card);
            key.style.backgroundColor = Accent;
            key.style.paddingLeft = 8;
            key.style.paddingRight = 8;
            key.style.paddingTop = 2;
            key.style.paddingBottom = 2;
            key.style.marginRight = 8;
            key.style.borderTopLeftRadius = 12;
            key.style.borderTopRightRadius = 12;
            key.style.borderBottomLeftRadius = 12;
            key.style.borderBottomRightRadius = 12;
            pill.Add(key);

            pill.Add(MakeLabel(hint.label, 13, Color.white));
            return pill;
        }

        private static Label MakeLabel(string text, int size, Color color)
        {
            var label = new Label(text);
            label.style.fontSize = size;
            label.style.color = color;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            return label;
        }

        private static VisualElement MakeIcon(ItemSO item, int quantity, float size)
        {
            var icon = new VisualElement();
            icon.style.width = size;
            icon.style.height = size;
            icon.style.marginRight = 4;
            icon.style.marginBottom = 4;
            icon.style.backgroundColor = new Color(0f, 0f, 0f, 0.35f);
            SetSprite(icon, item.itemIcon);

            if (quantity > 1)
            {
                Label count = MakeLabel($"x{quantity}", 11, Color.white);
                count.style.position = Position.Absolute;
                count.style.right = 1;
                count.style.bottom = 0;
                icon.Add(count);
            }

            return icon;
        }

        private static void SetSprite(VisualElement element, Sprite sprite)
        {
            element.style.backgroundImage = sprite != null
                ? new StyleBackground(sprite)
                : new StyleBackground(StyleKeyword.None);
            element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        }

        private static void SetPadding(VisualElement element, float value)
        {
            element.style.paddingTop = value;
            element.style.paddingBottom = value;
            element.style.paddingLeft = value;
            element.style.paddingRight = value;
        }

        private static void SetBorder(VisualElement element, float width, Color color, float radius)
        {
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;

            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;

            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }
    }
}