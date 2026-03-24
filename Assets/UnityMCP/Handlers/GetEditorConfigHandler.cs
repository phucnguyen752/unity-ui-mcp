using System.IO;
using UnityEngine;

namespace UnityMCP.Handlers
{
    public static class GetEditorConfigHandler
    {
        public static object Execute()
        {
            int tw = MCPBridgeWindow.TargetWidth;
            int th = MCPBridgeWindow.TargetHeight;
            var gameViewSize = GetGameViewSize();

            // Clear vision_json.json to prevent AI from copying old layout
            ClearVisionJson();

            return new
            {
                target_screen = new
                {
                    width = tw,
                    height = th
                },
                game_view = new
                {
                    width = (int)gameViewSize.x,
                    height = (int)gameViewSize.y
                },
                output_path = MCPBridgeWindow.OutputPath,
                vision_json_path = BuildUiFromJsonHandler.VisionJsonPath,
                sprite_path = GetSpritePath(),
                sizing_rule = "ALL sizes MUST be analyzed from the reference image. sizeDelta = target_screen × measured_ratio%. DO NOT use any default/example values. Measure each element independently.",
                workflow = "1) Write JSON layout to vision_json_path (project root)  2) Call build_ui_from_json (save_path defaults to output_path). Use sprite_path for rounded corners."
            };
        }

        private static void ClearVisionJson()
        {
            var fullPath = BuildUiFromJsonHandler.GetVisionJsonFullPath();
            File.WriteAllText(fullPath, "{}");
        }

        private static string GetSpritePath()
        {
            // Check UPM package path first
            var upmPath = "Packages/com.phucnguyen752.unity-ui-mcp/Sprites/circle-512.png";
            if (UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(upmPath) != null)
                return upmPath;

            // Fallback: local Assets path
            return "Assets/UnityMCP/Sprites/circle-512.png";
        }

        private static Vector2 GetGameViewSize()
        {
            try
            {
                var gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
                if (gameViewType != null)
                {
                    var window = UnityEditor.EditorWindow.GetWindow(gameViewType, false, null, false);
                    if (window != null)
                    {
                        var pos = window.position;
                        return new Vector2(pos.width, pos.height);
                    }
                }
            }
            catch { }
            return new Vector2(MCPBridgeWindow.TargetWidth, MCPBridgeWindow.TargetHeight);
        }
    }
}
