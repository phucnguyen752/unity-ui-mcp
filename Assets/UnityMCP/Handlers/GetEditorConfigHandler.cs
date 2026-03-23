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
                sizing_rule = "ALL sizes MUST be analyzed from the reference image. sizeDelta = target_screen × measured_ratio%. DO NOT use any default/example values. Measure each element independently."
            };
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
