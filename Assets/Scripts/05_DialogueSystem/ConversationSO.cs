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
        public string dialogueText;          
        
        [Tooltip("Línea de voz (AudioClip) asociado a la línea de díalogo.")]
        public AudioClip voiceLine;
        
        [Tooltip("Hasta 4 opciones de respuesta. Dejar answerText vacío para desactivar el slot.")]
        public AnswerOption[] answers = new AnswerOption[4];
        
        [Tooltip("Conversación a reproducir en caso de no respuesta dentro del tiempo limite.")]
        public ConversationSO noResponseConversation;
        
        [Tooltip("Tiempo que el jugador tiene para escoger una respuesta.")]
        public float answerTimeout = 6f;
        
        public bool HasAnswers()
        {
            if (answers == null) return false;
            foreach (var choice in answers)
                if (!string.IsNullOrEmpty(choice.answerText)) return true;
            return false;
        }
    }
    
    [System.Serializable]
    public struct AnswerOption
    {
        public string answerText;
        public ConversationSO nextConversation;
    }
}