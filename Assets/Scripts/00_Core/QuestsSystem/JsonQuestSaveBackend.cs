using System;
using System.IO;
using UnityEngine;

namespace Core.QuestsSystem
{
    /// <summary>
    /// Stores quest data as a JSON file in Application.persistentDataPath.
    /// Writes to a temp file first so a crash cannot leave a half-written save.
    /// </summary>
    public class JsonQuestSaveBackend : IQuestSaveBackend
    {
        private readonly string path;

        /// <param name="fileName">File name inside persistentDataPath.</param>
        public JsonQuestSaveBackend(string fileName = "quests.json")
        {
            path = Path.Combine(Application.persistentDataPath, fileName);
        }

        public QuestSaveData Load()
        {
            if (!File.Exists(path)) return new QuestSaveData();

            try
            {
                var data = JsonUtility.FromJson<QuestSaveData>(File.ReadAllText(path));
                return data ?? new QuestSaveData();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Quests] Failed to read save, starting empty. {e.Message}");
                try { File.Copy(path, path + ".corrupt", true); } catch { /* best effort */ }
                return new QuestSaveData();
            }
        }

        public void Save(QuestSaveData data)
        {
            try
            {
                string tempPath = path + ".tmp";
                File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));

                if (File.Exists(path)) File.Replace(tempPath, path, null);
                else File.Move(tempPath, path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Quests] Failed to write save. {e.Message}");
            }
        }
    }
}