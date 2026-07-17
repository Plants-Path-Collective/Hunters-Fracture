using System;
using System.Collections;
using Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

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

        private IEnumerator PlayLine(DialogueLine line)
        {
            // Show text based on display mode
            bool showText = (displayMode == DisplayMode.TextOnly || displayMode == DisplayMode.Both);
            bool playVoice = (displayMode == DisplayMode.VoiceOnly || displayMode == DisplayMode.Both);

            if (showText && dialoguePanel.dialogueText != null)
                dialoguePanel.dialogueText.text = line.dialogueText;

            if (playVoice && audioSource != null && line.voiceLine != null)
            {
                audioSource.clip = line.voiceLine;
                audioSource.Play();
                yield return new WaitForSeconds(line.voiceLine.length);
            }
            else if (showText && !playVoice)
            {
                // Estimate duration based on text if there is no voice line
                float duration = Mathf.Max(2f, line.dialogueText.Length * 0.06f);
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
            ShowAnswers(line);

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

        private void ShowAnswers(DialogueLine line)
        {
            if (dialoguePanel.answerPanel == null) return;

            dialoguePanel.answerPanel.SetActive(true);
            if (dialoguePanel.answerLabels == null) return;

            for (int i = 0; i < dialoguePanel.answerLabels.Length; i++)
            {
                bool active = i < line.answers.Length && !string.IsNullOrEmpty(line.answers[i].answerText);
                var label = dialoguePanel.answerLabels[i];
                if (label != null)
                {
                    label.gameObject.SetActive(active);
                    if (active) label.text = line.answers[i].answerText;
                }
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