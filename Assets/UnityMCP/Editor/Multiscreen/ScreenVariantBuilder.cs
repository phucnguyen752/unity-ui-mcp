using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace UnityMCP
{
    /// <summary>
    /// Builds screen-size variants of a UI tree.
    /// Each variant is a separate prefab saved to Assets/UI/Prefabs/Variants/.
    /// The Canvas CanvasScaler is adjusted per variant so the layout
    /// scales correctly on each screen form-factor.
    /// </summary>
    public class ScreenVariantBuilder
    {
        private readonly ComponentNode _tree;
        private const string PrefabFolder = "Assets/UI/Prefabs/Variants";

        public ScreenVariantBuilder(ComponentNode tree) => _tree = tree;

        public void BuildPortrait()   => Build("Portrait",   new Vector2(1080, 1920), 0f);
        public void BuildLandscape()  => Build("Landscape",  new Vector2(1920, 1080), 1f);
        public void BuildTablet()     => Build("Tablet",     new Vector2(1600, 1200), 0.5f);

        private void Build(string suffix, Vector2 resolution, float matchWidthOrHeight)
        {
            EditorApplication.delayCall += () =>
            {
                int undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName($"MCP: Build {suffix} variant");

                var root   = BuildCanvas(_tree.name + "_" + suffix, resolution, matchWidthOrHeight);
                BuildNode(_tree, root.transform, resolution);

                // Save as prefab
                System.IO.Directory.CreateDirectory(PrefabFolder);
                var path = $"{PrefabFolder}/{_tree.name}_{suffix}.prefab";
                PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.AutomatedAction);

                Undo.CollapseUndoOperations(undoGroup);
                Debug.Log($"[MCP] Saved variant: {path}");
            };
        }

        private static GameObject BuildCanvas(string name, Vector2 resolution, float match)
        {
            var go     = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = resolution;
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = match;

            go.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(go, "Create variant canvas");
            return go;
        }

        private void BuildNode(ComponentNode node, Transform parent, Vector2 resolution)
        {
            var go = CreateElementTool.Create(node, parent);
            SetAnchorTool.Apply(go, node.anchor, resolution);
            SetSizeTool.Apply(go, node.size, resolution);
            SetStyleTool.Apply(go, node);

            if (node.layout != LayoutType.None)
                SetLayoutTool.Apply(go, node);

            foreach (var child in node.children)
                BuildNode(child, go.transform, resolution);
        }
    }
}
