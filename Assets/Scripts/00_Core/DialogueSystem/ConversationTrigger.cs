using UnityEngine;
using PlantsPathCo.DialogueSystem;
using Player;

[RequireComponent(typeof(Collider))]
public class ConversationTrigger : MonoBehaviour, IPlayerInteractable
{
    [ReadOnly] public bool playing;
    
    [Header("NPC")]
    [SerializeField] private int npcID;

    [Header("Conversation")]
    [SerializeField] private ConversationSO[] conversations;

    [Tooltip("Bloquea el avance del índice mientras exista una misión pendiente.")]
    [SerializeField] private bool request;

    [Tooltip("Indice de la conversación a reproducir.")]
    [ReadOnly] [SerializeField] private int conversationIndex;

    /// <summary>Stable NPC identifier, shared with the quest system.</summary>
    public int NpcID => npcID;

    private string SaveKey => $"NPC_{npcID}_Conversation";
    private bool playerInRange;

    // Optional provider (e.g. NpcQuestGiver) that can replace the regular conversation.
    private IConversationOverride conversationOverride;
    private bool playingOverride;

    public Transform InteractionPoint => transform;

    private void Awake()
    {
        conversationIndex = PlayerPrefs.GetInt(SaveKey, 0);
        conversationOverride = GetComponent<IConversationOverride>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }

    public void Interact()
    {
        if (!playerInRange || playing)
            return;

        ConversationSO conversation;
        bool isOverride = false;

        // An override (quests) has priority over the NPC's regular conversations.
        if (conversationOverride != null && conversationOverride.TryGetConversation(out ConversationSO overridden))
        {
            conversation = overridden;
            isOverride = true;
        }
        else
        {
            if (conversations == null || conversations.Length == 0)
                return;

            int index = Mathf.Clamp(conversationIndex, 0, conversations.Length - 1);
            conversation = conversations[index];
        }

        if (conversation == null || conversation.lines == null || conversation.lines.Length == 0)
        {
            Debug.LogWarning($"[{nameof(ConversationTrigger)}] No valid conversation configured for '{name}'.", this);
            return;
        }

        if (DialogueManager.Instance == null)
        {
            Debug.LogError($"[{nameof(ConversationTrigger)}] No DialogueManager found; cannot start conversation.", this);
            return;
        }

        DialogueManager.Instance.StartConversation(conversation, this);
        playingOverride = isOverride;
        playing = true;
    }

    public void AdvanceConversation()
    {
        // Override conversations (quests) never move the regular index.
        if (playingOverride)
        {
            playingOverride = false;
            return;
        }

        if (request || conversations == null)
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