using System.Collections.Generic;
using UnityEngine;

namespace UnityMCP.Core
{
    /// <summary>
    /// Maps string IDs (returned to AI) ↔ actual GameObjects in the Editor.
    /// Also tracks active prefab sessions (prefab_id → root GameObject).
    /// </summary>
    public static class ElementRegistry
    {
        // element_id → GameObject
        private static readonly Dictionary<string, GameObject> _elements = new();

        // prefab_id → (rootGO, savePath)
        private static readonly Dictionary<string, (GameObject root, string savePath)> _prefabs = new();

        private static int _counter = 0;

        // ── Element ───────────────────────────────────────────────────────────

        public static string Register(GameObject go)
        {
            string id = $"elem_{++_counter:D4}";
            _elements[id] = go;
            return id;
        }

        public static GameObject GetElement(string id)
        {
            _elements.TryGetValue(id, out var go);
            return go;
        }

        public static bool HasElement(string id) => _elements.ContainsKey(id);

        // ── Prefab session ────────────────────────────────────────────────────

        public static string RegisterPrefab(GameObject root, string savePath)
        {
            string id = $"prefab_{++_counter:D4}";
            _prefabs[id] = (root, savePath);
            return id;
        }

        public static bool TryGetPrefab(string id, out GameObject root, out string savePath)
        {
            if (_prefabs.TryGetValue(id, out var entry))
            {
                root = entry.root;
                savePath = entry.savePath;
                return true;
            }
            root = null;
            savePath = null;
            return false;
        }

        public static void RemovePrefab(string id)
        {
            _prefabs.Remove(id);
        }

        // ── Cleanup ───────────────────────────────────────────────────────────

        public static void Clear()
        {
            _elements.Clear();
            _prefabs.Clear();
            _counter = 0;
        }
    }
}
