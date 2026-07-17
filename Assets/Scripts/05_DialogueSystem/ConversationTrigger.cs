using UnityEngine;
using PlantsPathCo.DialogueSystem;

[RequireComponent(typeof(Collider))]
public class ConversationTrigger : MonoBehaviour
{
    [Header("NPC")]
    [SerializeField] private int npcID;

    [Header("Conversation")]
    [SerializeField] private ConversationSO[] conversations;

    [Tooltip("Bloquea el avance del índice mientras exista una misión pendiente.")]
    [SerializeField] private bool request;

    [Tooltip("Indice de la conversación a reproducir.")]
    private int conversationIndex;

    private string SaveKey => $"NPC_{npcID}_Conversation";

    private void Awake()
    {
        conversationIndex = PlayerPrefs.GetInt(SaveKey, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        StartConversation();
    }

    private void StartConversation()
    {
        if (conversations == null || conversations.Length == 0)
            return;

        int index = Mathf.Clamp(conversationIndex, 0, conversations.Length - 1);

        DialogueManager.Instance.StartConversation(conversations[index]);
    }

    public void AdvanceConversation()
    {
        if (request)
            return;

        if (conversationIndex < conversations.Length - 1)
        {
            conversationIndex++;

            PlayerPrefs.SetInt(SaveKey, conversationIndex);
            PlayerPrefs.Save();
        }
    }

    public void SetRequest(bool value)
    {
        request = value;
    }

    public void ResetConversation()
    {
        conversationIndex = 0;
        PlayerPrefs.DeleteKey(SaveKey);
    }
}