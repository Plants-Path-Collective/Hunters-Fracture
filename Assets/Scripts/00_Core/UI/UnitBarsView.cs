using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core.CombatSystem.Units;
using Core.CombatSystem.StatusSystem;
using System.Collections.Generic;

namespace Core.UI
{
    /// <summary>
    /// World-space HP/SP bars that live on the Unit prefab and bind themselves to the Unit above
    /// them in the hierarchy: every Unit that CombatController spawns brings its own bars, so
    /// nothing needs to assign them. Lives on the world-space Canvas object itself.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class UnitBarsView : MonoBehaviour
    {
        [Header("--- Identity ---")]
        [SerializeField] private TMP_Text nameLabel;

        [Header("--- Health ---")]
        [SerializeField] private Image hpFill;
        [Tooltip("Optional.")]
        [SerializeField] private TMP_Text hpText;

        [Header("--- Energy ---")]
        [Tooltip("Root of the whole SP bar. Hidden when the Unit has no SP (generic enemies).")]
        [SerializeField] private GameObject spRoot;
        [SerializeField] private Image spFill;
        [Tooltip("Optional.")]
        [SerializeField] private TMP_Text spText;

        [Header("--- Statuses ---")]
        public GridLayoutGroup statusesContainer;
        [SerializeField] private GameObject statusPrefab;
        public List<StatusIcon> activeStatuses = new List<StatusIcon>();

        [Header("--- Behaviour ---")]
        [SerializeField, Min(0f)] private float fillDuration = 0.25f;
        [Tooltip("Keeps the bars parallel to the screen, whatever the Unit's rotation.")]
        [SerializeField] private bool faceCamera = true;
        [SerializeField] private bool hideWhenDead = true;

        private static readonly IReadOnlyList<StatusInstance> NoStatuses = new List<StatusInstance>();

        private Unit unit;
        private Canvas canvas;
        private Tween hpTween;
        private Tween spTween;
        private bool firstFillDone;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
        }

        private void OnEnable()
        {
            unit = GetComponentInParent<Unit>();
            if (unit == null)
            {
                Debug.LogError($"[{nameof(UnitBarsView)}] No Unit found in the parents of '{name}'.");
                return;
            }

            // Subscribe only. Reading stats here is not safe yet: Unit.Awake may not have run.
            unit.OnResourcesChanged += Refresh;
            unit.OnStatusesChanged += RefreshStatuses;
        }

        private void Start()
        {
            if (unit != null)
            {
                Refresh(unit);
                RefreshStatuses(unit);
            }
            if (nameLabel != null) nameLabel.text = unit.Name;
        }

        private void OnDisable()
        {
            if (unit != null)
            {
                unit.OnResourcesChanged -= Refresh;
                unit.OnStatusesChanged -= RefreshStatuses;
            }

            hpTween?.Kill();
            spTween?.Kill();
        }

        private void LateUpdate()
        {
            if (!faceCamera) return;

            Camera cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }

        private void Refresh(Unit changed)
        {
            // Stats are not ready until the Unit has been initialized (MaxHP is 0 before that).
            if (unit.StatsController == null || unit.MaxHP <= 0) return;

            bool animate = firstFillDone; // first valid refresh snaps instead of animating from 0
            firstFillDone = true;

            canvas.enabled = unit.IsAlive || !hideWhenDead;

            SetFill(hpFill, unit.HP / (float)unit.MaxHP, ref hpTween, animate);
            if (hpText != null) hpText.text = $"{unit.HP}/{unit.MaxHP}";

            bool hasSp = unit.MaxSP > 0;
            if (spRoot != null) spRoot.SetActive(hasSp);

            if (hasSp)
            {
                SetFill(spFill, unit.SP / (float)unit.MaxSP, ref spTween, animate);
                if (spText != null) spText.text = $"{unit.SP}/{unit.MaxSP}";
            }
        }

        /// <summary>
        /// Syncs the icons with the statuses the unit has right now. It diffs against the tracker's
        /// current list instead of reacting to each event, so it does not matter which change fired it
        /// (applied, refreshed, expired, countdown, death) or in what order: the result is the same.
        /// </summary>
        private void RefreshStatuses(Unit changed)
        {
            if (statusesContainer == null || statusPrefab == null) return;

            // Combat is null outside an encounter (before EnterCombat, after ExitCombat).
            IReadOnlyList<StatusInstance> current =
                unit.Combat != null ? unit.Combat.GetStatuses(unit) : NoStatuses;

            // Icons whose status is gone. Removed from the list right away: the fade-out takes a moment,
            // and the same status applied again meanwhile must get a fresh icon, not this fading one.
            for (int i = activeStatuses.Count - 1; i >= 0; i--)
            {
                StatusIcon icon = activeStatuses[i];
                if (!HasStatus(current, icon.Definition))
                {
                    activeStatuses.RemoveAt(i);
                    icon.Remove();
                }
            }

            // New statuses get an icon; existing ones just update their counter.
            for (int i = 0; i < current.Count; i++)
            {
                StatusInstance instance = current[i];
                StatusIcon icon = FindIcon(instance.definition);

                if (icon != null)
                {
                    icon.SetTurns(instance.definition.GetDisplayCount(instance));
                    continue;
                }

                GameObject go = Instantiate(statusPrefab, statusesContainer.transform);
                icon = go.GetComponent<StatusIcon>();
                icon.SetStatus(instance.definition, instance.definition.GetDisplayCount(instance));
                activeStatuses.Add(icon);
            }
        }

        private StatusIcon FindIcon(StatusSO definition)
        {
            for (int i = 0; i < activeStatuses.Count; i++)
                if (activeStatuses[i].Definition == definition) return activeStatuses[i];
            return null;
        }

        private static bool HasStatus(IReadOnlyList<StatusInstance> statuses, StatusSO definition)
        {
            for (int i = 0; i < statuses.Count; i++)
                if (statuses[i].definition == definition) return true;
            return false;
        }

        private void SetFill(Image fill, float target, ref Tween tween, bool animate)
        {
            if (fill == null) return;

            tween?.Kill();
            target = Mathf.Clamp01(target);

            if (!animate || fillDuration <= 0f)
            {
                fill.fillAmount = target;
                return;
            }

            tween = DOTween.To(() => fill.fillAmount, v => fill.fillAmount = v, target, fillDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
        }
    }
}