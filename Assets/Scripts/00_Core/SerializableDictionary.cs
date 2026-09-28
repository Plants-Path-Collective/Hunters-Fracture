using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// A real Dictionary that Unity can serialize (stored as two parallel lists).
    /// Use it for data that must live in the Inspector / assets, like inventory snapshots.
    /// </summary>
    /// <remarks>
    /// Rows with a repeated key are dropped on deserialize. Until a custom drawer exists,
    /// adding several rows in the Inspector is awkward — fill entries from code instead.
    /// </remarks>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> keys = new();
        [SerializeField] private List<TValue> values = new();

        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();

            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            Clear();

            int count = Mathf.Min(keys.Count, values.Count);
            for (int i = 0; i < count; i++)
            {
                if (keys[i] == null || ContainsKey(keys[i])) continue;
                Add(keys[i], values[i]);
            }
        }
    }
}