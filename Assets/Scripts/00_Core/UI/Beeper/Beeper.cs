using UnityEngine;
using DG.Tweening;

namespace Core.UI.Beeper
{
    public class Beeper : MonoBehaviour
    {
        [SerializeField] private GameObject beeperVisuals;

        [Header("Juice (Placeholder)")]
        [SerializeField, Min(0f)] private float introDuration = 0.35f;
        [SerializeField, Min(0f)] private float outroDuration = 0.35f;
        [SerializeField] private float hiddenPositionY = -1080f;

        [Header("--- Panels ---")]
        [SerializeField] private GameObject mainPanel;
        [Space(10)]
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject missionPanel;
        [SerializeField] private GameObject agendaPanel;
        [SerializeField] private GameObject mapsPanel;
        [SerializeField] private GameObject wikiPanel;

        // Private References
        private RectTransform visualsRect;
        private Tween positionTween;
        private bool isVisible;

        private void Awake()
        {
            if (beeperVisuals == null)
            {
                Debug.LogError($"[{nameof(Beeper)}] Beeper visuals are not assigned.", this);
                return;
            }

            visualsRect = beeperVisuals.GetComponent<RectTransform>();
            if (visualsRect == null)
                Debug.LogError($"[{nameof(Beeper)}] Beeper visuals must have a " +
                               $"{nameof(RectTransform)}.", beeperVisuals);
        }

        public void Toggle()
        {
            if (!ValidateVisuals())
                return;

            isVisible = !isVisible;
            positionTween?.Kill();

            if (isVisible)
            {
                beeperVisuals.SetActive(true);
                SetVisualsY(hiddenPositionY);
                positionTween = DOTween.To(
                        () => visualsRect.anchoredPosition.y,
                        SetVisualsY,
                        0f,
                        introDuration)
                    .SetEase(Ease.OutCubic)
                    .SetTarget(this)
                    .SetLink(gameObject);
                return;
            }

            positionTween = DOTween.To(
                    () => visualsRect.anchoredPosition.y,
                    SetVisualsY,
                    hiddenPositionY,
                    outroDuration)
                .SetEase(Ease.InCubic)
                .SetTarget(this)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    positionTween = null;
                    if (!isVisible)
                        beeperVisuals.SetActive(false);
                });
        }

        public void Hide()
        {
            if (!ValidateVisuals())
                return;

            positionTween?.Kill();
            positionTween = null;
            isVisible = false;
            SetVisualsY(hiddenPositionY);
            beeperVisuals.SetActive(false);
        }

        public void OpenOptions() => OpenPanel(optionsPanel, nameof(optionsPanel));

        public void OpenMissions() => OpenPanel(missionPanel, nameof(missionPanel));

        public void OpenAgenda() => OpenPanel(agendaPanel, nameof(agendaPanel));

        public void OpenMaps() => OpenPanel(mapsPanel, nameof(mapsPanel));

        public void OpenWiki() => OpenPanel(wikiPanel, nameof(wikiPanel));

        private void OpenPanel(GameObject panel, string panelName)
        {
            if (mainPanel == null)
            {
                Debug.LogError($"[{nameof(Beeper)}] {nameof(mainPanel)} is not assigned.", this);
                return;
            }

            if (panel == null)
            {
                Debug.LogError($"[{nameof(Beeper)}] {panelName} is not assigned.", this);
                return;
            }

            mainPanel.SetActive(true);
            SetPanelActive(optionsPanel, panel);
            SetPanelActive(missionPanel, panel);
            SetPanelActive(agendaPanel, panel);
            SetPanelActive(mapsPanel, panel);
            SetPanelActive(wikiPanel, panel);
            panel.SetActive(true);
        }

        private static void SetPanelActive(GameObject candidate, GameObject selectedPanel)
        {
            if (candidate != null && candidate != selectedPanel)
                candidate.SetActive(false);
        }

        private bool ValidateVisuals()
        {
            if (beeperVisuals == null)
            {
                Debug.LogError($"[{nameof(Beeper)}] Beeper visuals are not assigned.", this);
                return false;
            }

            if (visualsRect == null)
            {
                Debug.LogError($"[{nameof(Beeper)}] Beeper visuals must have a " +
                               $"{nameof(RectTransform)}.", beeperVisuals);
                return false;
            }

            return true;
        }

        private void SetVisualsY(float y)
        {
            Vector2 position = visualsRect.anchoredPosition;
            position.y = y;
            visualsRect.anchoredPosition = position;
        }
    }
}