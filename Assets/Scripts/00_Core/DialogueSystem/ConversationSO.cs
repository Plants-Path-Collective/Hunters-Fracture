using UnityEngine;
using UnityEngine.Localization;
using PlantsPathCo.DialogueSystem;

namespace PlantsPathCo.DialogueSystem
{
    [CreateAssetMenu(fileName = "NewConversation", menuName = "Dialogue System/Conversation")]
    public class ConversationSO : ScriptableObject
    {
        [Tooltip("Líneas en orden de reproducción.")]
        public DialogueLine[] lines;
    }

    [System.Serializable]
    public class DialogueLine
    {
        [Tooltip("Personaje que dice esta línea. Si se deja vacío, se mantiene el último " +
                 "hablante mostrado (útil para varias líneas seguidas del mismo personaje).")]
        public SpeakerSO speaker;

        [Tooltip("Texto localizado de la línea. La traducción real vive en la String Table; " +
                 "este campo solo referencia la tabla + la key de la entrada.")]
        public LocalizedString dialogueText;

        [Tooltip("Línea de voz asociada a esta línea de diálogo. Es completamente opcional " +
                 "(dejar vacía es válido); es el DialogueManager quien decide en runtime, según " +
                 "el Display Mode, si necesita reproducir voz o no. Localizada, para poder tener " +
                 "(o no tener) doblaje distinto por idioma.")]
        public LocalizedAudioClip voiceLine;

        [Tooltip("Hasta 4 opciones de respuesta. Dejar la LocalizedString vacía para desactivar el slot.")]
        public AnswerOption[] answers = new AnswerOption[4];

        [Tooltip("Conversación a reproducir en caso de no respuesta dentro del tiempo limite.")]
        public ConversationSO noResponseConversation;

        [Tooltip("Tiempo que el jugador tiene para escoger una respuesta.")]
        public float answerTimeout = 6f;

        public bool HasAnswers()
        {
            if (answers == null) return false;
            foreach (var choice in answers)
                if (choice.answerText != null && !choice.answerText.IsEmpty) return true;
            return false;
        }
    }

    [System.Serializable]
    public struct AnswerOption
    {
        public LocalizedString answerText;
        public ConversationSO nextConversation;
    }
}