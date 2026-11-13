#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Text;
using DevTools;
using UnityEngine.Scripting;
using Core.ChallengesSystem;
using Core.QuestsSystem;

namespace DevTools
{
    /// <summary>
    /// Console commands to test quests without dialogue or UI.
    /// </summary>
    [Preserve]
    internal static class QuestCommands
    {
        private const string NotReady = "QuestManager is not available in this scene.";

        [ConsoleCommand("quest.list", "Lists every quest in the catalog with its state.")]
        private static string ListQuests()
        {
            QuestManager manager = QuestManager.Instance;
            if (manager == null) return NotReady;

            var sb = new StringBuilder();
            foreach (QuestSO quest in manager.Catalog.Quests)
            {
                if (quest == null) continue;
                if (sb.Length > 0) sb.Append('\n');
                sb.Append($"{quest.questID}: {manager.GetState(quest)}");
            }

            return sb.Length > 0 ? sb.ToString() : "The catalog is empty.";
        }

        [ConsoleCommand("quest.state", "Shows the state and step progress of a quest.")]
        private static string State(string questID)
        {
            QuestManager manager = QuestManager.Instance;
            if (manager == null) return NotReady;
            if (!manager.Catalog.TryGet(questID, out QuestSO quest)) return $"Unknown quest '{questID}'.";

            var sb = new StringBuilder($"{quest.questID}: {manager.GetState(quest)}");
            for (int i = 0; i < quest.steps.Count; i++)
            {
                (int current, int target) = manager.GetStepProgress(quest, i);
                sb.Append($"\n  [{i}] {quest.steps[i].stepID}: {current}/{target} ({quest.steps[i].milestoneID})");
            }

            return sb.ToString();
        }

        [ConsoleCommand("quest.accept", "Accepts a quest without dialogue (offer window and prerequisites still apply).")]
        private static string Accept(string questID)
        {
            QuestManager manager = QuestManager.Instance;
            if (manager == null) return NotReady;

            return manager.Accept(questID, out string reason)
                ? $"Accepted '{questID}'."
                : $"Cannot accept: {reason}";
        }

        [ConsoleCommand("quest.turnin", "Turns in a ready quest without dialogue (turn-in window still applies).")]
        private static string TurnIn(string questID)
        {
            QuestManager manager = QuestManager.Instance;
            if (manager == null) return NotReady;

            return manager.TryTurnIn(questID, out string reason)
                ? $"Turned in '{questID}'."
                : $"Cannot turn in: {reason}";
        }

        [ConsoleCommand("quest.progress", "Reports a milestone manually, e.g. quest.progress talk:npc_01")]
        private static string Progress(string milestoneID)
        {
            ChallengeEvents.ReportMilestone(milestoneID);
            return $"Reported milestone '{milestoneID}'.";
        }

        [ConsoleCommand("quest.reset", "Clears all quest state and progress (quest side of BeginRun).")]
        private static string Reset()
        {
            QuestManager manager = QuestManager.Instance;
            if (manager == null) return NotReady;

            manager.BeginRun();
            return "Quests reset.";
        }
    }
}
#endif