using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// A real Dictionary that Unity can serialize, stored as a single list of key/value rows.
    /// Use it for data that must live in the Inspector / assets, like inventory snapshots.
    /// </summary>
    /// <remarks>
    /// <para>The rows are what the Inspector edits. A row added with "+" starts as a copy of the
    /// last one (same key): it stays visible but has no effect until you give it a unique key.
    /// When two rows share a key, the first one wins.</para>
    /// <para>The rows are only rebuilt from the dictionary when the dictionary was changed from
    /// code, so half-edited rows in the Inspector are never lost.</para>
    /// </remarks>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [Serializable]
        public struct Entry
        {
            public TKey key;
            public TValue value;
        }

        [SerializeField] private List<Entry> entries = new();

        public void OnBeforeSerialize()
        {
            // Dictionary untouched since the last deserialize: keep the rows exactly as authored.
            if (MatchesEntries()) return;

            entries.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in this)
                entries.Add(new Entry { key = pair.Key, value = pair.Value });
        }

        public void OnAfterDeserialize()
        {
            Clear();

            foreach (Entry entry in entries)
            {
                if (entry.key == null || ContainsKey(entry.key)) continue;
                Add(entry.key, entry.value);
            }
        }

        /// <summary>True if the dictionary is exactly what the rows describe (first row wins per key).</summary>
        private bool MatchesEntries()
        {
            var seen = new HashSet<TKey>();

            foreach (Entry entry in entries)
            {
                if (entry.key == null || !seen.Add(entry.key)) continue;

                if (!TryGetValue(entry.key, out TValue current) ||
                    !EqualityComparer<TValue>.Default.Equals(current, entry.value))
                    return false;
            }

            return seen.Count == Count;
        }
    }
}