using System;
using System.Collections.Generic;
using UnityEngine;

namespace DVG.UniTools
{
    public class Database<K, V> : ScriptableObject
        where V : class
    {
        public const string Menu = "Data/Database/";

        [SerializeField]
        private List<Data> _items = new();

        private readonly Dictionary<K, V> _lookup = new();

        private bool _lookupInited;

        private void OnValidate()
        {
            _lookupInited = false;
        }

        private void InitLookup()
        {
            if (_lookupInited)
                return;

            _lookup.Clear();
            for (int i = 0; i < _items.Count; i++)
            {
                Data item = _items[i];
                _lookup.Add(item.Key, item.Value);
            }
        }

        public V Get(K key)
        {
            InitLookup();
            _lookup.TryGetValue(key, out V value);
            return value;
        }

        [Serializable]
        private class Data
        {
            public K Key;
            public V Value;
        }
    }
}
