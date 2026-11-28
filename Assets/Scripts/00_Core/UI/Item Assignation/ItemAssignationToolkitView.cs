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
    /// Layout: header, party cards (one per slot, key badge on top), unassigned list + description, footer hints.
    /// Texts are hardcoded and not localized yet.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ItemAssignationToolkitView : ItemAssignationView
    {
        private sealed class MemberRow
        {
            public VisualElement root;
            public VisualElement keyHolder;
            public VisualElement portrait;
            public Label nameLabel;
            public VisualElement items;
        }

        private static readonly Color Dim = new(0f, 0f, 0f, 0.7f);
        private static readonly Color Card = new(0.09f, 0.10f, 0.13f, 1f);
        private static readonly Color Slot = new(0.15f, 0.17f, 0.21f, 1f);
        private static readonly Color SlotSelected = new(0.22f, 0.24f, 0.30f, 1f);
        private static readonly Color Accent = new(1f, 0.78f, 0.25f, 1f);
        private static readonly Color Muted = new(0.65f, 0.68f, 0.74f, 1f);
        private static readonly Color Line = new(1f, 1f, 1f, 0.10f);

        private UIDocument document;
        private VisualElement overlay;
        private Label counterLabel;
        private ScrollView unassignedScroll;
        private VisualElement unassignedList;
        private VisualElement holdFill;
        private VisualElement descIcon;
        private Label descName;
        private Label descText;
        private VisualElement pillBar;
        private readonly MemberRow[] rows = new MemberRow[AllyParty.MaxMembers];
        private bool visible;

        [Header("--- Input glyphs ---")]
        [SerializeField] private InputGlyphLibrary glyphs;

        private readonly List<Sprite> glyphBuffer = new();
        private IReadOnlyList<InputHint> lastHints;

        // ── Lyfecycle ───────────────────────────────────────────────

        private void Awake() => document = GetComponent<UIDocument>();

        private void OnEnable()
        {
            InputEvent.RegisterListener();
            InputEvent.InputTypeChanged += OnInputTypeChanged;
        }

        private void OnDisable()
        {
            InputEvent.InputTypeChanged -= OnInputTypeChanged;
            InputEvent.UnregisterListener();
        }

        // Swap keyboard <-> gamepad glyphs live.
        private void OnInputTypeChanged(InputEvent.InputType type)
        {
            if (lastHints != null) SetHints(lastHints);
        }

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

            int left = session.Unassigned.Count;
            counterLabel.text = left > 0 ? $"{left} left to assign" : "All items assigned";
            counterLabel.style.color = left > 0 ? Muted : Accent;

            RenderUnassigned(session);

            for (int slot = 0; slot < rows.Length; slot++)
                RenderMember(session, slot);

            ItemSO selected = session.Selected;
            descName.text = selected != null ? selected.localizedName.GetLocalizedString() : string.Empty;
            descText.text = selected != null ? selected.localizedDescription.GetLocalizedString() : string.Empty;
            descIcon.style.display = selected != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (selected != null) SetSprite(descIcon, selected.itemIcon);
        }

        public override void SetHints(IReadOnlyList<InputHint> hints)
        {
            if (!EnsureBuilt()) return;

            lastHints = hints;
            pillBar.Clear();
            foreach (InputHint hint in hints)
            {
                if (hint.slot >= 0)
                {
                    if (hint.slot < rows.Length) ApplySlotHint(rows[hint.slot], hint);
                    continue;
                }

                if (hint.enabled) pillBar.Add(MakePill(hint));
            }
        }

        private void ApplySlotHint(MemberRow row, InputHint hint)
        {
            FillKey(row.keyHolder, hint, 28);
            row.keyHolder.style.visibility = hint.enabled ? Visibility.Visible : Visibility.Hidden;
        }
        
        public override void SetHoldProgress(float normalized)
        {
            if (!EnsureBuilt()) return;
            holdFill.style.width = Length.Percent(Mathf.Clamp01(normalized) * 100f);
        }

        // ── Rendering ─────────────────────────────────────────────────────────

        private void RenderUnassigned(ItemAssignmentSession session)
        {
            unassignedList.Clear();

            if (session.Unassigned.Count == 0)
            {
                Label empty = MakeLabel("Nothing left to assign.", 13, Muted);
                empty.style.unityFontStyleAndWeight = FontStyle.Normal;
                empty.style.marginTop = 8;
                unassignedList.Add(empty);
                return;
            }

            VisualElement selectedEntry = null;

            for (int i = 0; i < session.Unassigned.Count; i++)
            {
                ItemSO item = session.Unassigned[i];
                bool selected = i == session.SelectedIndex;

                var entry = new VisualElement();
                entry.style.flexDirection = FlexDirection.Row;
                entry.style.alignItems = Align.Center;
                entry.style.backgroundColor = selected ? SlotSelected : Slot;
                entry.style.marginBottom = 4;
                SetPadding(entry, 6);
                SetBorder(entry, 2, selected ? Accent : Color.clear, 6);

                entry.Add(MakeIcon(item, 1, 36));

                Label name = MakeLabel(item.localizedName.GetLocalizedString(), 14, selected ? Accent : Color.white);
                name.style.marginLeft = 4;
                entry.Add(name);

                unassignedList.Add(entry);
                if (selected) selectedEntry = entry;
            }

            // Keep the selection visible; layout is only valid on the next frame.
            if (selectedEntry != null)
            {
                VisualElement target = selectedEntry;
                target.schedule.Execute(() => unassignedScroll.ScrollTo(target));
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
            bool any = false;
            foreach ((ItemSO item, int quantity) in session.GetStacks(slot))
            {
                if (item == null || quantity <= 0) continue;
                row.items.Add(MakeIcon(item, quantity, 36));
                any = true;
            }

            if (!any)
            {
                Label none = MakeLabel("No items", 12, Muted);
                none.style.unityFontStyleAndWeight = FontStyle.Normal;
                row.items.Add(none);
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

            var card = new VisualElement();
            card.style.width = Length.Percent(85);
            card.style.maxWidth = 960;
            card.style.backgroundColor = Card;
            SetPadding(card, 20);
            SetBorder(card, 2, Muted, 10);
            overlay.Add(card);

            BuildHeader(card);
            BuildPartyRow(card);
            BuildBottomRow(card);
            BuildFooter(card);
        }

        private void BuildHeader(VisualElement card)
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 14;
            card.Add(header);

            header.Add(MakeLabel("ASSIGN ITEMS", 22, Accent));

            counterLabel = MakeLabel(string.Empty, 14, Muted);
            header.Add(counterLabel);
        }

        private void BuildPartyRow(VisualElement card)
        {
            // Same left / center / right order as the assign keys.
            var party = new VisualElement();
            party.style.flexDirection = FlexDirection.Row;
            card.Add(party);

            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = BuildMemberRow();
                if (i > 0) rows[i].root.style.marginLeft = 12;
                party.Add(rows[i].root);
            }
        }

        private void BuildBottomRow(VisualElement card)
        {
            var bottom = new VisualElement();
            bottom.style.flexDirection = FlexDirection.Row;
            bottom.style.marginTop = 16;
            card.Add(bottom);

            // Left: unassigned items (scrolls when there are many).
            var left = new VisualElement();
            left.style.width = Length.Percent(42);
            left.Add(MakeLabel("Unassigned", 13, Muted));

            unassignedScroll = new ScrollView(ScrollViewMode.Vertical);
            unassignedScroll.style.maxHeight = 190;
            unassignedScroll.style.marginTop = 6;
            unassignedScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            unassignedList = unassignedScroll.contentContainer;
            left.Add(unassignedScroll);
            bottom.Add(left);

            // Right: description of the selected item.
            var desc = new VisualElement();
            desc.style.flexGrow = 1;
            desc.style.flexDirection = FlexDirection.Row;
            desc.style.marginLeft = 16;
            desc.style.minHeight = 110;
            desc.style.backgroundColor = Slot;
            SetPadding(desc, 14);
            SetBorder(desc, 1, Line, 8);
            bottom.Add(desc);

            descIcon = new VisualElement();
            descIcon.style.width = 56;
            descIcon.style.height = 56;
            descIcon.style.marginRight = 14;
            descIcon.style.flexShrink = 0;
            descIcon.style.backgroundColor = new Color(0f, 0f, 0f, 0.35f);
            descIcon.style.display = DisplayStyle.None;
            desc.Add(descIcon);

            var texts = new VisualElement();
            texts.style.flexGrow = 1;
            texts.style.flexShrink = 1;
            desc.Add(texts);

            descName = MakeLabel(string.Empty, 18, Color.white);
            texts.Add(descName);

            descText = MakeLabel(string.Empty, 14, Muted);
            descText.style.unityFontStyleAndWeight = FontStyle.Normal;
            descText.style.whiteSpace = WhiteSpace.Normal;
            descText.style.marginTop = 4;
            texts.Add(descText);
        }

        private void BuildFooter(VisualElement card)
        {
            // Hold-to-confirm track is always reserved so the layout never jumps.
            var holdBar = new VisualElement();
            holdBar.style.height = 4;
            holdBar.style.marginTop = 16;
            holdBar.style.backgroundColor = Line;
            holdBar.style.borderTopLeftRadius = 2;
            holdBar.style.borderTopRightRadius = 2;
            holdBar.style.borderBottomLeftRadius = 2;
            holdBar.style.borderBottomRightRadius = 2;
            card.Add(holdBar);

            holdFill = new VisualElement();
            holdFill.style.height = Length.Percent(100);
            holdFill.style.width = Length.Percent(0);
            holdFill.style.backgroundColor = Accent;
            holdBar.Add(holdFill);

            pillBar = new VisualElement();
            pillBar.style.flexDirection = FlexDirection.Row;
            pillBar.style.flexWrap = Wrap.Wrap;
            pillBar.style.justifyContent = Justify.Center;
            pillBar.style.marginTop = 12;
            pillBar.style.minHeight = 30; // keeps the card height stable while pills come and go
            card.Add(pillBar);
        }

        private MemberRow BuildMemberRow()
        {
            var row = new MemberRow();

            row.root = new VisualElement();
            row.root.style.flexGrow = 1;
            row.root.style.flexBasis = 0; // equal width for every card
            row.root.style.alignItems = Align.Center;
            row.root.style.backgroundColor = Slot;
            SetPadding(row.root, 12);
            SetBorder(row.root, 1, Line, 8);

            row.keyHolder = new VisualElement();
            row.keyHolder.style.flexDirection = FlexDirection.Row;
            row.keyHolder.style.alignItems = Align.Center;
            row.keyHolder.style.justifyContent = Justify.Center;
            row.keyHolder.style.minHeight = 28;
            row.keyHolder.style.marginBottom = 10;
            row.keyHolder.style.visibility = Visibility.Hidden;
            row.root.Add(row.keyHolder);

            row.portrait = new VisualElement();
            row.portrait.style.width = 96;
            row.portrait.style.height = 96;
            row.portrait.style.backgroundColor = new Color(0f, 0f, 0f, 0.35f);
            SetRadius(row.portrait, 8);
            row.root.Add(row.portrait);

            row.nameLabel = MakeLabel(string.Empty, 16, Color.white);
            row.nameLabel.style.marginTop = 8;
            row.root.Add(row.nameLabel);

            var divider = new VisualElement();
            divider.style.alignSelf = Align.Stretch;
            divider.style.height = 1;
            divider.style.marginTop = 8;
            divider.style.marginBottom = 8;
            divider.style.backgroundColor = Line;
            row.root.Add(divider);

            row.items = new VisualElement();
            row.items.style.flexDirection = FlexDirection.Row;
            row.items.style.flexWrap = Wrap.Wrap;
            row.items.style.justifyContent = Justify.Center;
            row.items.style.minHeight = 40;
            row.root.Add(row.items);

            return row;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        // Rounded pill: key badge + action label.
        private VisualElement MakePill(InputHint hint)
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
            SetBorder(pill, 1, Muted, 16);

            var key = new VisualElement();
            key.style.flexDirection = FlexDirection.Row;
            key.style.alignItems = Align.Center;
            key.style.marginRight = 8;
            FillKey(key, hint, 22);
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
            SetRadius(icon, 4);
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

        private static void SetRadius(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
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

            SetRadius(element, radius);
        }

        // Glyph sprite(s) for the current device, or the old text badge if the library has none for this control.
        private void FillKey(VisualElement holder, InputHint hint, float size)
        {
            holder.Clear();
            InputGlyphs.Resolve(hint.action, InputEvent.CurrentInputType, glyphs, glyphBuffer);

            if (glyphBuffer.Count > 0)
            {
                holder.style.backgroundColor = Color.clear;
                holder.style.minWidth = 0;
                SetPadding(holder, 0);
                SetRadius(holder, 0);

                foreach (Sprite sprite in glyphBuffer)
                {
                    var icon = new VisualElement();
                    icon.style.width = size;
                    icon.style.height = size;
                    icon.style.marginRight = 2;
                    SetSprite(icon, sprite);
                    holder.Add(icon);
                }
                return;
            }

            holder.style.backgroundColor = Accent;
            holder.style.minWidth = 24;
            holder.style.paddingLeft = 8;
            holder.style.paddingRight = 8;
            holder.style.paddingTop = 2;
            holder.style.paddingBottom = 2;
            SetRadius(holder, 12);
            holder.Add(MakeLabel(hint.key, 12, Card));
        }
    }
}