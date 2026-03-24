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

                    // Root canvas
                    _rootGo = CreateCanvas(tree.name, targetResolution);
                    BuildNode(tree, _rootGo.transform, targetResolution);

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

        // ── Canvas factory ────────────────────────────────────
        private static GameObject CreateCanvas(string name, Vector2 resolution)
        {
            var go     = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode         = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = resolution;
            scaler.screenMatchMode     = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = resolution.x > resolution.y ? 1f : 0f; // landscape=width, portrait=height

            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
            return go;
        }
    }
}
