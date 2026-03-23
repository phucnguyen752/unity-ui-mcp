using System.IO;
using UnityEditor;
using UnityEngine;
using UnityMCP.Core;

namespace UnityMCP
{
    /// <summary>
    /// EditorWindow: Window > MCP Bridge
    /// Cho phép bật/tắt MCP Server (SSE transport), cấu hình target screen và xem trạng thái.
    /// </summary>
    public class MCPBridgeWindow : EditorWindow
    {
        private static McpHttpServer _server;
        private Vector2 _scroll;
        private string _log = "";

        // ── Target Screen Config ────────────────────────────────────────
        private const string PREF_TARGET_WIDTH  = "UnityMCP_TargetWidth";
        private const string PREF_TARGET_HEIGHT = "UnityMCP_TargetHeight";

        private int _targetWidth  = 1080;
        private int _targetHeight = 1920;

        // Expose cho các handler/tool khác đọc
        public static int TargetWidth  { get; private set; } = 1080;
        public static int TargetHeight { get; private set; } = 1920;

        // ── Claude Rules status ─────────────────────────────────────────
        private bool _rulesInstalled;

        [MenuItem("Window/MCP Bridge")]
        public static void Open()
        {
            var win = GetWindow<MCPBridgeWindow>("MCP Bridge");
            win.minSize = new Vector2(320, 460);
        }

        private void OnEnable()
        {
            _targetWidth  = EditorPrefs.GetInt(PREF_TARGET_WIDTH, 1080);
            _targetHeight = EditorPrefs.GetInt(PREF_TARGET_HEIGHT, 1920);
            TargetWidth  = _targetWidth;
            TargetHeight = _targetHeight;
            _rulesInstalled = CheckRulesInstalled();
        }

        private void OnGUI()
        {
            GUILayout.Label("Unity UI MCP Server", EditorStyles.boldLabel);
            GUILayout.Space(4);

            bool running = _server?.IsRunning ?? false;

            // Status indicator
            var color = running ? Color.green : Color.gray;
            var oldColor = GUI.color;
            GUI.color = color;
            GUILayout.Label(running
                ? $"● Running  (http://localhost:{McpHttpServer.Port}/sse)"
                : "● Stopped", EditorStyles.label);
            GUI.color = oldColor;

            GUILayout.Space(8);

            if (!running)
            {
                if (GUILayout.Button("Start MCP Server", GUILayout.Height(32)))
                {
                    _server = new McpHttpServer();
                    _server.Start();
                    AppendLog($"MCP Server started on port {McpHttpServer.Port}.");
                }
            }
            else
            {
                if (GUILayout.Button("Stop MCP Server", GUILayout.Height(32)))
                {
                    _server.Stop();
                    AppendLog("MCP Server stopped.");
                }
            }

            GUILayout.Space(8);

            // ── Target Screen ───────────────────────────────────────────
            GUILayout.Label("Target Screen", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Đặt resolution thiết kế chuẩn. AI sẽ dùng giá trị này để tính toán kích thước UI chính xác.",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Width", GUILayout.Width(50));
            _targetWidth = EditorGUILayout.IntField(_targetWidth, GUILayout.Width(80));
            EditorGUILayout.LabelField("×", GUILayout.Width(15));
            EditorGUILayout.LabelField("Height", GUILayout.Width(50));
            _targetHeight = EditorGUILayout.IntField(_targetHeight, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            // Save khi thay đổi
            if (_targetWidth != TargetWidth || _targetHeight != TargetHeight)
            {
                TargetWidth  = _targetWidth;
                TargetHeight = _targetHeight;
                EditorPrefs.SetInt(PREF_TARGET_WIDTH, _targetWidth);
                EditorPrefs.SetInt(PREF_TARGET_HEIGHT, _targetHeight);
            }

            GUILayout.Space(8);

            // ── Claude Rules Setup ──────────────────────────────────────
            DrawClaudeRulesSection();

            GUILayout.Space(8);

            // Config hint
            EditorGUILayout.HelpBox(
                "Claude Desktop config:\n" +
                "{\n" +
                "  \"mcpServers\": {\n" +
                "    \"unity-ui-mcp\": {\n" +
                $"      \"url\": \"http://localhost:{McpHttpServer.Port}/sse\"\n" +
                "    }\n" +
                "  }\n" +
                "}",
                MessageType.Info);

            GUILayout.Space(8);
            GUILayout.Label("Log", EditorStyles.boldLabel);
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(100));
            GUILayout.TextArea(_log, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();

            GUILayout.Space(4);
            if (GUILayout.Button("Clear Log")) _log = "";

            // Auto-refresh
            Repaint();
        }

        // ── Claude Rules ────────────────────────────────────────────────

        private void DrawClaudeRulesSection()
        {
            GUILayout.Label("Claude Rules", EditorStyles.boldLabel);

            _rulesInstalled = CheckRulesInstalled();

            if (_rulesInstalled)
            {
                var oldBg = GUI.color;
                GUI.color = Color.green;
                GUILayout.Label("✓ Rules đã cài đặt", EditorStyles.label);
                GUI.color = oldBg;

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Update Rules", EditorStyles.miniButtonLeft))
                {
                    InstallRules();
                    AppendLog("Claude rules updated.");
                }
                if (GUILayout.Button("Remove Rules", EditorStyles.miniButtonRight))
                {
                    RemoveRules();
                    AppendLog("Claude rules removed.");
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Claude Code cần rules để tạo UI chính xác (kích thước, bo góc, workflow).\nBấm Install để tự động cài đặt.",
                    MessageType.Warning);

                if (GUILayout.Button("Install Claude Rules", GUILayout.Height(28)))
                {
                    InstallRules();
                    AppendLog("Claude rules installed.");
                }
            }
        }

        private static string GetProjectRoot()
        {
            // Application.dataPath = ".../Assets" → parent = project root
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static string GetRulesDestDir()
        {
            return Path.Combine(GetProjectRoot(), ".claude", "rules");
        }

        private static string GetRulesDestFile()
        {
            return Path.Combine(GetRulesDestDir(), "unity-ui-mcp.md");
        }

        private static string GetRulesSourceFile()
        {
            // Tìm CLAUDE.md trong package UnityMCP
            var guids = AssetDatabase.FindAssets("CLAUDE t:TextAsset", new[] { "Assets/UnityMCP" });
            if (guids.Length > 0)
                return Path.GetFullPath(AssetDatabase.GUIDToAssetPath(guids[0]));

            // Fallback: path trực tiếp
            var direct = Path.Combine(Application.dataPath, "UnityMCP", "CLAUDE.md");
            return File.Exists(direct) ? direct : null;
        }

        private static bool CheckRulesInstalled()
        {
            return File.Exists(GetRulesDestFile());
        }

        private static void InstallRules()
        {
            var src = GetRulesSourceFile();
            if (src == null || !File.Exists(src))
            {
                Debug.LogError("[UnityMCP] Cannot find CLAUDE.md in Assets/UnityMCP/");
                return;
            }

            var destDir = GetRulesDestDir();
            Directory.CreateDirectory(destDir);

            File.Copy(src, GetRulesDestFile(), overwrite: true);
            Debug.Log($"[UnityMCP] Claude rules installed → {GetRulesDestFile()}");
        }

        private static void RemoveRules()
        {
            var dest = GetRulesDestFile();
            if (File.Exists(dest))
            {
                File.Delete(dest);
                Debug.Log("[UnityMCP] Claude rules removed.");
            }
        }

        // ── Logging ─────────────────────────────────────────────────────

        private void AppendLog(string msg)
        {
            var time = System.DateTime.Now.ToString("HH:mm:ss");
            _log = $"[{time}] {msg}\n" + _log;
            if (_log.Length > 4000) _log = _log[..4000];
        }

        private void OnDisable()
        {
            // Không tự stop khi đóng window — server vẫn chạy
        }
    }
}
