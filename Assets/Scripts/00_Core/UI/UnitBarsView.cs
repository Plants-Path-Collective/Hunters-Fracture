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
            unit.OnIndicatorsChanged += RefreshStatuses;
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
                unit.OnIndicatorsChanged -= RefreshStatuses;
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

        private readonly struct IconEntry
        {
            public readonly object key; public readonly Sprite sprite; public readonly int count; public readonly Color tint;
            public IconEntry(object key, Sprite sprite, int count, Color tint)
            { this.key = key; this.sprite = sprite; this.count = count; this.tint = tint; }
        }
        private readonly List<IconEntry> wanted = new();

        private void RefreshStatuses(Unit changed)
        {
            if (statusesContainer == null || statusPrefab == null) return;

            wanted.Clear();
            if (unit.Combat != null)
                foreach (StatusInstance s in unit.Combat.GetStatuses(unit))
                    wanted.Add(new IconEntry(s.definition, s.definition.icon, s.definition.GetDisplayCount(s), Color.white));
            foreach (IndicatorEntry e in unit.Indicators)
                wanted.Add(new IconEntry(e.definition, e.definition.icon, e.count, e.definition.tint));

            for (int i = activeStatuses.Count - 1; i >= 0; i--)
            {
                StatusIcon icon = activeStatuses[i];
                if (!Wants(icon.Key))
                {
                    activeStatuses.RemoveAt(i);
                    icon.Remove();
                }
            }

            foreach (IconEntry entry in wanted)
            {
                StatusIcon icon = FindIcon(entry.key);
                if (icon != null) { icon.SetTurns(entry.count); continue; }

                GameObject go = Instantiate(statusPrefab, statusesContainer.transform);
                icon = go.GetComponent<StatusIcon>();
                icon.Show(entry.key, entry.sprite, entry.count, entry.tint);
                activeStatuses.Add(icon);
            }
        }

        private StatusIcon FindIcon(object key)
        {
            for (int i = 0; i < activeStatuses.Count; i++)
                if (activeStatuses[i].Key == key) return activeStatuses[i];
            return null;
        }

        private bool Wants(object key)
        {
            for (int i = 0; i < wanted.Count; i++)
                if (wanted[i].key == key) return true;
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