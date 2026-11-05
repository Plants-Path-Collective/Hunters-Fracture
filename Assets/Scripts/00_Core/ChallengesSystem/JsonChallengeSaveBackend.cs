using System;
using System.IO;
using UnityEngine;

namespace Core.ChallengesSystem
{
    /// <summary>
    /// Stores challenge data as a JSON file in Application.persistentDataPath.
    /// Writes to a temp file first so a crash cannot leave a half-written save.
    /// </summary>
    public class JsonChallengeSaveBackend : IChallengeSaveBackend
    {
        private readonly string path;

        /// <param name="fileName">File name inside persistentDataPath.</param>
        public JsonChallengeSaveBackend(string fileName = "challenges.json")
        {
            path = Path.Combine(Application.persistentDataPath, fileName);
        }

        public ChallengeSaveData Load()
        {
            if (!File.Exists(path)) return new ChallengeSaveData();

            try
            {
                var data = JsonUtility.FromJson<ChallengeSaveData>(File.ReadAllText(path));
                return data ?? new ChallengeSaveData();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Challenges] Failed to read save, starting empty. {e.Message}");
                try { File.Copy(path, path + ".corrupt", true); } catch { /* best effort */ }
                return new ChallengeSaveData();
            }
        }

        public void Save(ChallengeSaveData data)
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
                Debug.LogError($"[Challenges] Failed to write save. {e.Message}");
            }
        }
    }
}