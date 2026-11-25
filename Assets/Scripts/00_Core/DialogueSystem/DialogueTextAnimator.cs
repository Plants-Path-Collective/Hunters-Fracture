using DG.Tweening;
using TMPro;
using UnityEngine;

namespace PlantsPathCo.DialogueSystem
{
    /// <summary>
    /// Typewriter reveal with a small hop + fade per character. Works on the TMP vertex data,
    /// so rich text tags and localized strings keep working.
    /// Attach to the same GameObject as the dialogue TextMeshProUGUI.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class DialogueTextAnimator : MonoBehaviour
    {
        [Header("Reveal")]
        [Tooltip("Characters revealed per second.")]
        [SerializeField] private float charsPerSecond = 40f;
        [Tooltip("Extra pause (seconds) after . , ! ? ; : and the ellipsis character.")]
        [SerializeField] private float punctuationPause = 0.12f;

        [Header("Per-character animation")]
        [Tooltip("How long each character takes to settle (seconds).")]
        [SerializeField] private float charAnimDuration = 0.22f;
        [Tooltip("Start offset below the baseline, in text units (roughly pixels).")]
        [SerializeField] private float hopHeight = 8f;
        [Tooltip("Easing of the hop. OutBack overshoots slightly above the baseline, which reads as a hop.")]
        [SerializeField] private Ease hopEase = Ease.OutBack;

        private TMP_Text tmp;
        private float[] revealTimes = System.Array.Empty<float>();
        private float lastRevealTime;
        private float elapsed;
        private bool animating;

        /// <summary>True while characters are still being revealed.</summary>
        public bool IsRevealing => animating && elapsed < lastRevealTime;

        /// <summary>True until the last character has finished its hop.</summary>
        public bool IsAnimating => animating;

        private void Awake() => tmp = GetComponent<TMP_Text>();

        private void OnDisable() => animating = false;

        /// <summary>Sets the text and starts the reveal. The object must be active.</summary>
        public void Play(string text)
        {
            if (tmp == null) tmp = GetComponent<TMP_Text>();

            tmp.text = text;
            tmp.ForceMeshUpdate();

            TMP_TextInfo info = tmp.textInfo;
            int count = info.characterCount;
            revealTimes = new float[count];

            float step = 1f / Mathf.Max(1f, charsPerSecond);
            float t = 0f;
            lastRevealTime = 0f;

            for (int i = 0; i < count; i++)
            {
                TMP_CharacterInfo ch = info.characterInfo[i];
                revealTimes[i] = t;
                if (!ch.isVisible) continue; // spaces don't cost time

                lastRevealTime = t;
                t += step;
                if (IsPausePunctuation(ch.character)) t += punctuationPause;
            }

            elapsed = 0f;
            animating = count > 0;
            if (animating) Apply(); // hides every character on this same frame (no flash of full text)
        }

        /// <summary>Instantly shows the full text and stops the animation.</summary>
        public void Complete()
        {
            if (!animating) return;
            elapsed = lastRevealTime + charAnimDuration;
            Finish();
        }

        private void LateUpdate()
        {
            if (!animating) return;

            elapsed += Time.deltaTime;
            Apply();

            if (elapsed >= lastRevealTime + charAnimDuration)
                Finish();
        }

        private void Apply()
        {
            // Rebuild a clean mesh every frame, then offset it. Cheap for one dialogue box,
            // and it keeps working if the layout changes while the text is animating.
            tmp.ForceMeshUpdate();
            TMP_TextInfo info = tmp.textInfo;
            int count = Mathf.Min(info.characterCount, revealTimes.Length);

            for (int i = 0; i < count; i++)
            {
                TMP_CharacterInfo ch = info.characterInfo[i];
                if (!ch.isVisible) continue;

                float p = Mathf.Clamp01((elapsed - revealTimes[i]) / charAnimDuration);
                float eased = DOVirtual.EasedValue(0f, 1f, p, hopEase);
                float yOffset = -(1f - eased) * hopHeight;
                float alpha = Mathf.Clamp01(p * 2f); // fully visible at half the animation

                int m = ch.materialReferenceIndex;
                int v = ch.vertexIndex;
                Vector3[] verts = info.meshInfo[m].vertices;
                Color32[] cols = info.meshInfo[m].colors32;

                for (int j = 0; j < 4; j++)
                {
                    verts[v + j].y += yOffset;

                    Color32 c = cols[v + j];
                    c.a = (byte)(c.a * alpha);
                    cols[v + j] = c;
                }
            }

            tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }

        private void Finish()
        {
            animating = false;
            tmp.ForceMeshUpdate(); // restores the untouched mesh
        }

        private static bool IsPausePunctuation(char c) =>
            c == '.' || c == ',' || c == '!' || c == '?' || c == ';' || c == ':' || c == '…';
    }
}