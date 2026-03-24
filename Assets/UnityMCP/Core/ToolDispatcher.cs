using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading.Tasks;

namespace UnityMCP
{
    /// <summary>
    /// Walks the ComponentNode tree and calls the appropriate Tool for each node.
    /// All EditorAPI calls are dispatched on the main thread via delayCall.
    /// </summary>
    public class ToolDispatcher
    {
        private GameObject _rootGo;
        private string _savedPrefabPath;

        /// <summary>Path of the saved prefab after BuildAsync completes (null if not saved).</summary>
        public string SavedPrefabPath => _savedPrefabPath;

        public Task BuildAsync(ComponentNode tree, Vector2 targetResolution,
                               string savePath = null, string prefabName = null)
        {
            var tcs = new TaskCompletionSource<bool>();

            EditorApplication.delayCall += () =>
            {
                try
                {
                    Undo.SetCurrentGroupName("MCP: Build UI");
                    int undoGroup = Undo.GetCurrentGroup();

                    // Build root from JSON tree (no Canvas wrapper - prefab is meant to be placed inside an existing Canvas)
                    _rootGo = BuildRootNode(tree, targetResolution);

                    Undo.CollapseUndoOperations(undoGroup);

                    // Auto-save as prefab
                    if (!string.IsNullOrEmpty(savePath))
                    {
                        var name = string.IsNullOrEmpty(prefabName) ? tree.name : prefabName;
                        _rootGo.name = name;
                        _savedPrefabPath = SaveAsPrefab(_rootGo, savePath, name);
                    }
                    else
                    {
                        Selection.activeGameObject = _rootGo;
                        EditorGUIUtility.PingObject(_rootGo);
                    }

                    tcs.SetResult(true);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[UnityMCP] Error building UI: {e}");
                    tcs.SetException(e);
                }
            };

            return tcs.Task;
        }

        private static string SaveAsPrefab(GameObject root, string folder, string name)
        {
            folder = folder.TrimEnd('/');
            if (!AssetDatabase.IsValidFolder(folder))
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "..", folder));

            var assetPath = $"{folder}/{name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            Object.DestroyImmediate(root);

            // Ping the saved prefab
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            Debug.Log($"[UnityMCP] Prefab saved: {assetPath}");
            return assetPath;
        }

        // ── Root builder (no Canvas) ─────────────────────────
        private GameObject BuildRootNode(ComponentNode tree, Vector2 resolution)
        {
            // Create a temporary parent so CreateElementTool has something to attach to
            var tempParent = new GameObject("_TempParent");
            Undo.RegisterCreatedObjectUndo(tempParent, "Create TempParent");
            tempParent.AddComponent<RectTransform>();

            // Build the root node using normal flow (same as children)
            var go = CreateElementTool.Create(tree, tempParent.transform);
            SetAnchorTool.Apply(go, tree.anchor, resolution);
            SetSizeTool.Apply(go, tree.size, resolution);
            SetStyleTool.Apply(go, tree);

            if (tree.position != null && (tree.position.x != 0 || tree.position.y != 0))
            {
                var rt = go.GetComponent<RectTransform>();
                if (rt != null)
                    rt.anchoredPosition = new Vector2(tree.position.x, tree.position.y);
            }

            if (tree.layout != LayoutType.None)
                SetLayoutTool.Apply(go, tree);

            foreach (var child in tree.children)
                BuildNode(child, go.transform, resolution);

            // Detach from temp parent and clean up
            go.transform.SetParent(null);
            Object.DestroyImmediate(tempParent);

            return go;
        }

        // ── Recursive node builder ────────────────────────────
        private void BuildNode(ComponentNode node, Transform parent, Vector2 resolution)
        {
            GameObject go = CreateElementTool.Create(node, parent);

            SetAnchorTool.Apply(go, node.anchor, resolution);
            SetSizeTool.Apply(go, node.size, resolution);
            SetStyleTool.Apply(go, node);

            // Apply anchoredPosition
            if (node.position != null && (node.position.x != 0 || node.position.y != 0))
            {
                var rt = go.GetComponent<RectTransform>();
                if (rt != null)
                    rt.anchoredPosition = new Vector2(node.position.x, node.position.y);
            }

            if (node.layout != LayoutType.None)
                SetLayoutTool.Apply(go, node);

            foreach (var child in node.children)
                BuildNode(child, go.transform, resolution);
        }
    }
}
