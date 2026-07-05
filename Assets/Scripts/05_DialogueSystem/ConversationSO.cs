using UnityEngine;

namespace PlantsPathCo.DialogueSystem
{
    [CreateAssetMenu(fileName = "NewConversation", menuName = "Dialogue/Conversation")]
    public class ConversationSO : ScriptableObject
    {
        [Tooltip("Líneas en orden de reproducción.")]
        public DialogueLine[] lines;
    }

    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 5)]
        public string dialogueText;          // Renombrado para claridad

        public AudioClip voiceLine;

        [Tooltip("Hasta 4 opciones de respuesta. Dejar answerText vacío para desactivar el slot.")]
        public AnswerOption[] answers = new AnswerOption[4];

        public ConversationSO noResponseConversation;
        public float answerTimeout = 6f;

        public bool HasAnswers()
        {
            if (answers == null) return false;
            foreach (var a in answers)
                if (!string.IsNullOrEmpty(a.answerText)) return true;
            return false;
        }
    }

    [System.Serializable]
    public struct AnswerOption
    {
        public string answerText;
        public ConversationSO nextConversation;
        // Se elimina fareModifier
    }
}