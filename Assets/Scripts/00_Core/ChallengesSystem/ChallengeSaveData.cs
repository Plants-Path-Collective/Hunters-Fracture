using System;
using System.Collections.Generic;

namespace Core.ChallengesSystem
{
    /// <summary>
    /// Progress of one challenge, keyed by its stable challengeID.
    /// </summary>
    [Serializable]
    public class ChallengeProgress
    {
        public string id;
        public int value;
        public bool completed;
    }

    /// <summary>
    /// Root object serialized to disk.
    /// </summary>
    [Serializable]
    public class ChallengeSaveData
    {
        /// <summary>Save format version, for future migrations.</summary>
        public int version = 1;
        public List<ChallengeProgress> progress = new();
        public List<string> unlocks = new();
    }
}