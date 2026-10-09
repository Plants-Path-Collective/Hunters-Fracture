using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Core.CombatSystem;
using Core.CombatSystem.SkillSystem;
using Core.CombatSystem.Units;

namespace Core.UI
{
    /// <summary>Skills list. Driven only by CombatInputHandler's menu events.</summary>
    public class SkillsMenuView : MonoBehaviour
    {
        [SerializeField] private CombatInputHandler inputHandler;
        [Tooltip("Root shown while the menu is open.")]
        [SerializeField] private GameObject panel;
        [Tooltip("Parent of the rows. Give it a VerticalLayoutGroup.")]
        [SerializeField] private RectTransform listRoot;
        [SerializeField] private GameObject slotPrefab;
        [Tooltip("Optional: description of the highlighted skill.")]
        [SerializeField] private TMP_Text descriptionLabel;

        private readonly List<SkillSlotView> rows = new();
        private IReadOnlyList<ActionSO> skills;

        private void OnEnable()
        {
            panel.SetActive(false);
            if (inputHandler == null) return;

            inputHandler.OnSkillsMenuOpened += Open;
            inputHandler.OnSkillsMenuIndexChanged += Select;
            inputHandler.OnSkillsMenuClosed += Close;
        }

        private void OnDisable()
        {
            if (inputHandler == null) return;

            inputHandler.OnSkillsMenuOpened -= Open;
            inputHandler.OnSkillsMenuIndexChanged -= Select;
            inputHandler.OnSkillsMenuClosed -= Close;
        }

        private void Open(Unit actor, IReadOnlyList<ActionSO> list, int selected)
        {
            skills = list;

            while (rows.Count < list.Count)
            {
                GameObject instance = Instantiate(slotPrefab, listRoot);
                SkillSlotView slotView = instance.GetComponent<SkillSlotView>();

                if (slotView == null)
                {
                    Debug.LogError($"{nameof(SkillsMenuView)} requires a {nameof(SkillSlotView)} component on the slot prefab.", this);
                    Destroy(instance);
                    break;
                }

                rows.Add(slotView);
            }

            for (int i = 0; i < rows.Count; i++)
            {
                bool used = i < list.Count;
                rows[i].gameObject.SetActive(used);
                if (used) rows[i].Bind(list[i], affordable: actor.SP >= list[i].spCost);
            }

            panel.SetActive(true);
            Select(selected);
        }

        private void Select(int index)
        {
            for (int i = 0; i < rows.Count; i++)
                rows[i].SetSelected(i == index);

            if (descriptionLabel != null && skills != null && index < skills.Count)
                descriptionLabel.text = skills[index].description;
        }

        private void Close() => panel.SetActive(false);
    }
}