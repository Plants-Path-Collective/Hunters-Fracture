using System;
using System.Collections;
using Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Localization;

namespace PlantsPathCo.DialogueSystem
{
    public enum DisplayMode
    {
        TextOnly,
        VoiceOnly,
        Both
    }

    [Serializable]
    public struct DialoguePanel
    {
        [Tooltip("Dialogue panel to show/hide when a conversation starts or ends.")]
        public GameObject panelRoot;

        [Tooltip("Body text to show the dialogue lines/subtitles.")]
        public TextMeshProUGUI dialogueText;

        [Tooltip("Panel that contains the different answers to continue a conversation.")]
        public GameObject answerPanel;

        [Tooltip("Texts for the different answers.")]
        public TextMeshProUGUI[] answerLabels;

        [Tooltip("A frame for the illustration of the character who is speaking.")]
        public UnityEngine.UI.Image portraitImage;

        [Tooltip("Text to show the name of the talker.")]
        public TextMeshProUGUI speakerNameText;
    }

    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance;

        [Tooltip("Choose whether to display conversations only in the UI, using voice lines, or using both.")]
        [SerializeField] private DisplayMode displayMode = DisplayMode.Both;
        [Tooltip("Reference to the conversation currently being played. Used internally to track the active dialogue flow.")]
        [SerializeField] private ConversationSO currentConversation;

        private ConversationTrigger currentTrigger;

        [Tooltip("Current line index being played within the active conversation. Resets when changing conversations.")]
        [SerializeField] private int lineIndex;

        [SerializeField] private DialoguePanel dialoguePanel;

        [Tooltip("Audio Source from which the voice lines will be played.")]
        [SerializeField] private AudioSource audioSource;

        [Header("Input Actions (up to 4)")]
        [SerializeField] private InputActionReference[] choiceInputs = new InputActionReference[4];

        [Header("Events")]
        public UnityEvent<ConversationSO> onConversationStart;
        public UnityEvent<ConversationSO> onConversationEnd;
        public UnityEvent<DialogueLine> onLineDisplayed;
        public UnityEvent<AnswerOption, int> onChoiceSelected;

        private Coroutine runningCoroutine;

        // Cache of currently active answer LocalizedStrings, in the same order as line.answers,
        // so we can map an input index back to the correct AnswerOption/nextConversation.
        private int[] activeAnswerIndices = new int[4];

        public bool IsTalking => runningCoroutine != null;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void OnEnable()
        {
            foreach (var input in choiceInputs)
                if (input != null) input.action.Enable();
        }

        private void OnDisable()
        {
            foreach (var input in choiceInputs)
                if (input != null) input.action.Disable();
        }

        /// <summary>Función para empezar una conversación, requiere de un ConversationSO.</summary>
        public void StartConversation(ConversationSO conversation, ConversationTrigger trigger)
        {
            if (conversation == null || conversation.lines == null || conversation.lines.Length == 0)
                return;

            if (runningCoroutine != null)
                StopCoroutine(runningCoroutine);

            currentConversation = conversation;
            currentTrigger = trigger;
            lineIndex = 0;

            InputManager.Instance.ChangeActionMap(INPUTACTION_MAP.Dialogue);

            if (dialoguePanel.panelRoot != null)
                dialoguePanel.panelRoot.SetActive(true);

            onConversationStart?.Invoke(conversation);
            runningCoroutine = StartCoroutine(PlayConversation());
        }

        /// <summary>Method to stop a conversation.</summary>
        public void StopConversation()
        {
            if (runningCoroutine != null)
                StopCoroutine(runningCoroutine);
            EndConversation();
        }

        private IEnumerator PlayConversation()
        {
            while (currentConversation != null && lineIndex < currentConversation.lines.Length)
            {
                DialogueLine line = currentConversation.lines[lineIndex];
                yield return PlayLine(line);

                if (line.HasAnswers())
                {
                    ConversationSO next = null;
                    yield return WaitForAnswer(line, result => next = result);

                    if (next != null)
                    {
                        currentConversation = next;
                        lineIndex = 0;
                        continue;
                    }
                    break;
                }

                lineIndex++;
            }

            EndConversation();
        }

        // Resolves a LocalizedString against the currently selected Locale.
        // Blocks the coroutine (not the whole game) until the string/table is loaded.
        private IEnumerator ResolveLocalizedString(LocalizedString localizedString, Action<string> onResolved)
        {
            if (localizedString == null || localizedString.IsEmpty)
            {
                onResolved(string.Empty);
                yield break;
            }

            var op = localizedString.GetLocalizedStringAsync();
            yield return op;
            onResolved(op.Result);
        }

        // Resolves a LocalizedAudioClip against the currently selected Locale.
        // Returns null if the field is empty (no voice line configured for this line/locale),
        // which is a perfectly valid state — voice lines are optional.
        private IEnumerator ResolveLocalizedAudioClip(LocalizedAudioClip localizedAudioClip, Action<AudioClip> onResolved)
        {
            if (localizedAudioClip == null || localizedAudioClip.IsEmpty)
            {
                onResolved(null);
                yield break;
            }

            var op = localizedAudioClip.LoadAssetAsync();
            yield return op;
            onResolved(op.Result);
        }

        private IEnumerator PlayLine(DialogueLine line)
        {
            if (line.speaker != null)
                yield return ShowSpeaker(line.speaker);

            string text = string.Empty;
            yield return ResolveLocalizedString(line.dialogueText, result => text = result);

            // Show text based on display mode
            bool showText = (displayMode == DisplayMode.TextOnly || displayMode == DisplayMode.Both);
            bool playVoice = (displayMode == DisplayMode.VoiceOnly || displayMode == DisplayMode.Both);

            if (showText && dialoguePanel.dialogueText != null)
                dialoguePanel.dialogueText.text = text;

            // La línea de voz es opcional: solo se intenta resolver si el Display Mode
            // actual la necesita. Que no haya clip (para este idioma o en general) es válido.
            AudioClip clip = null;
            if (playVoice)
                yield return ResolveLocalizedAudioClip(line.voiceLine, result => clip = result);

            if (playVoice && audioSource != null && clip != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
                yield return new WaitForSeconds(clip.length);
            }
            else if (showText)
            {
                // Sin clip que reproducir (no había voz para esta línea/idioma, o el modo es TextOnly):
                // se estima la duración según la longitud del texto.
                float duration = Mathf.Max(2f, text.Length * 0.06f);
                yield return new WaitForSeconds(duration);
            }
            else
            {
                yield return null;
            }

            onLineDisplayed?.Invoke(line);
        }

        private IEnumerator WaitForAnswer(DialogueLine line, Action<ConversationSO> callback)
        {
            yield return ShowAnswers(line);

            float timer = 0f;
            int chosenIndex = -1;

            while (timer < line.answerTimeout)
            {
                for (int i = 0; i < choiceInputs.Length; i++)
                {
                    if (choiceInputs[i] != null && choiceInputs[i].action.WasPressedThisFrame())
                    {
                        chosenIndex = i;
                        break;
                    }
                }
                if (chosenIndex >= 0) break;

                timer += Time.deltaTime;
                yield return null;
            }

            HideAnswers();

            if (chosenIndex >= 0 && chosenIndex < line.answers.Length)
            {
                AnswerOption chosen = line.answers[chosenIndex];
                onChoiceSelected?.Invoke(chosen, chosenIndex);
                callback(chosen.nextConversation);
            }
            else
            {
                callback(line.noResponseConversation);
            }
        }

        private IEnumerator ShowSpeaker(SpeakerSO speaker)
        {
            if (dialoguePanel.portraitImage != null)
                dialoguePanel.portraitImage.sprite = speaker.portrait;

            if (dialoguePanel.speakerNameText != null)
            {
                string name = string.Empty;
                yield return ResolveLocalizedString(speaker.speakerName, result => name = result);
                dialoguePanel.speakerNameText.text = name;
            }
        }

        private IEnumerator ShowAnswers(DialogueLine line)
        {
            if (dialoguePanel.answerPanel == null) yield break;

            dialoguePanel.answerPanel.SetActive(true);
            if (dialoguePanel.answerLabels == null) yield break;

            for (int i = 0; i < dialoguePanel.answerLabels.Length; i++)
            {
                bool active = i < line.answers.Length &&
                              line.answers[i].answerText != null &&
                              !line.answers[i].answerText.IsEmpty;

                var label = dialoguePanel.answerLabels[i];
                if (label == null) continue;

                label.gameObject.SetActive(active);
                if (!active) continue;

                string answerText = string.Empty;
                yield return ResolveLocalizedString(line.answers[i].answerText, result => answerText = result);
                label.text = answerText;
            }
        }

        private void HideAnswers()
        {
            if (dialoguePanel.answerPanel != null)
                dialoguePanel.answerPanel.SetActive(false);
        }

        private void EndConversation()
        {
            HideAnswers();

            if (dialoguePanel.dialogueText != null)
                dialoguePanel.dialogueText.text = string.Empty;

            if (dialoguePanel.panelRoot != null)
                dialoguePanel.panelRoot.SetActive(false);

            if (currentTrigger != null)
            {
                currentTrigger.playing = false;
                currentTrigger.AdvanceConversation();
            }

            onConversationEnd?.Invoke(currentConversation);

            currentConversation = null;
            runningCoroutine = null;

            InputManager.Instance.ChangeActionMap(INPUTACTION_MAP.Exploration);
        }
    }
}