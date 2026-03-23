using UnityEngine;
using UnityEditor;
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

        public Task BuildAsync(ComponentNode tree, Vector2 targetResolution)
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

                    // Generate C# references file
                    UIReferencesGenerator.Generate(_rootGo, tree);

                    Undo.CollapseUndoOperations(undoGroup);
                    Selection.activeGameObject = _rootGo;
                    EditorGUIUtility.PingObject(_rootGo);

                    tcs.SetResult(true);
                }
                catch (System.Exception e)
                {
                    tcs.SetException(e);
                }
            };

            return tcs.Task;
        }

        // ── Recursive node builder ────────────────────────────
        private void BuildNode(ComponentNode node, Transform parent, Vector2 resolution)
        {
            GameObject go = CreateElementTool.Create(node, parent);

            SetAnchorTool.Apply(go, node.anchor, resolution);
            SetSizeTool.Apply(go, node.size, resolution);
            SetStyleTool.Apply(go, node);

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
