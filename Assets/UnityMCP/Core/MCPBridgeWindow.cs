using System.IO;
using UnityEditor;
using UnityEngine;
using UnityMCP.Core;

namespace UnityMCP
{
    /// <summary>
    /// EditorWindow: Window > MCP Bridge
    /// Start/stop MCP Server (SSE transport), configure target screen, output path, and view status.
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

        // Exposed for handlers/tools to read
        public static int TargetWidth  { get; private set; } = 1080;
        public static int TargetHeight { get; private set; } = 1920;

        // ── Output Path Config ──────────────────────────────────────────
        private const string PREF_OUTPUT_PATH = "UnityMCP_OutputPath";
        private string _outputPath = "Assets/UI/Prefabs";
        public static string OutputPath { get; private set; } = "Assets/UI/Prefabs";

        // ── AI Skill status ─────────────────────────────────────────────
        public enum IdeType { Cursor, Windsurf, ClaudeDesktop, McpPrompt }
        private IdeType _selectedIde = IdeType.Cursor;
        private bool _rulesInstalled;

        [MenuItem("Window/MCP Bridge")]
        public static void Open()
        {
            var win = GetWindow<MCPBridgeWindow>("MCP Bridge");
            win.minSize = new Vector2(320, 520);
        }

        private void OnEnable()
        {
            _targetWidth  = EditorPrefs.GetInt(PREF_TARGET_WIDTH, 1080);
            _targetHeight = EditorPrefs.GetInt(PREF_TARGET_HEIGHT, 1920);
            TargetWidth  = _targetWidth;
            TargetHeight = _targetHeight;

            _outputPath = EditorPrefs.GetString(PREF_OUTPUT_PATH, "Assets/UI/Prefabs");
            OutputPath = _outputPath;

            _rulesInstalled = CheckRulesInstalled(_selectedIde);
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
                "Set the design resolution. AI will use this to calculate UI sizes accurately.",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Width", GUILayout.Width(50));
            _targetWidth = EditorGUILayout.IntField(_targetWidth, GUILayout.Width(80));
            EditorGUILayout.LabelField("×", GUILayout.Width(15));
            EditorGUILayout.LabelField("Height", GUILayout.Width(50));
            _targetHeight = EditorGUILayout.IntField(_targetHeight, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            // Save on change
            if (_targetWidth != TargetWidth || _targetHeight != TargetHeight)
            {
                TargetWidth  = _targetWidth;
                TargetHeight = _targetHeight;
                EditorPrefs.SetInt(PREF_TARGET_WIDTH, _targetWidth);
                EditorPrefs.SetInt(PREF_TARGET_HEIGHT, _targetHeight);
            }

            GUILayout.Space(8);

            // ── Output Path ─────────────────────────────────────────────
            GUILayout.Label("Output Path", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Folder where built prefabs will be saved.",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();
            _outputPath = EditorGUILayout.TextField(_outputPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                var selected = EditorUtility.OpenFolderPanel("Select Output Folder", _outputPath, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    // Convert absolute path to relative Assets/ path
                    var dataPath = Path.GetFullPath(Application.dataPath);
                    var fullSelected = Path.GetFullPath(selected);
                    if (fullSelected.StartsWith(dataPath))
                        _outputPath = "Assets" + fullSelected.Substring(dataPath.Length).Replace('\\', '/');
                    else
                        _outputPath = fullSelected.Replace('\\', '/');
                }
            }
            EditorGUILayout.EndHorizontal();

            // Save on change
            if (_outputPath != OutputPath)
            {
                OutputPath = _outputPath;
                EditorPrefs.SetString(PREF_OUTPUT_PATH, _outputPath);
            }

            GUILayout.Space(8);

            // ── AI Skill / Rules Setup ──────────────────────────────────
            DrawSkillSetupSection();

            GUILayout.Space(8);

            // Config hint
            string configJson =
                "{\n" +
                "  \"mcpServers\": {\n" +
                "    \"unity-ui-mcp\": {\n" +
                $"      \"serverUrl\": \"http://localhost:{McpHttpServer.Port}/sse\"\n" +
                "    }\n" +
                "  }\n" +
                "}";

            EditorGUILayout.HelpBox($"Config:\n{configJson}", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy JSON Config"))
            {
                EditorGUIUtility.systemCopyBuffer = configJson;
                AppendLog("MCP JSON Config copied to clipboard.");
            }
            if (GUILayout.Button("Copy SSE URL"))
            {
                EditorGUIUtility.systemCopyBuffer = $"http://localhost:{McpHttpServer.Port}/sse";
                AppendLog("MCP SSE URL copied to clipboard.");
            }
            EditorGUILayout.EndHorizontal();

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

        // ── AI Skill Setup ──────────────────────────────────────────────

        private void DrawSkillSetupSection()
        {
            GUILayout.Label("AI Skill / Rules Setup", EditorStyles.boldLabel);

            _selectedIde = (IdeType)EditorGUILayout.EnumPopup("Target IDE:", _selectedIde);

            if (_selectedIde == IdeType.McpPrompt)
            {
                EditorGUILayout.HelpBox("Uses MCP Prompts protocol (unity_ui_expert_skill). No file copy needed. The IDE will request this skill via MCP (if supported).", MessageType.Info);
                return;
            }

            _rulesInstalled = CheckRulesInstalled(_selectedIde);

            if (_rulesInstalled)
            {
                var oldBg = GUI.color;
                GUI.color = Color.green;
                GUILayout.Label($"✓ Rules installed for {_selectedIde}", EditorStyles.label);
                GUI.color = oldBg;

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Update Rules", EditorStyles.miniButtonLeft))
                {
                    InstallRules(_selectedIde);
                    AppendLog($"{_selectedIde} rules updated.");
                }
                if (GUILayout.Button("Remove Rules", EditorStyles.miniButtonRight))
                {
                    RemoveRules(_selectedIde);
                    AppendLog($"{_selectedIde} rules removed.");
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"Rules file ({GetDestFileName(_selectedIde)}) is required for AI to generate UI accurately.",
                    MessageType.Warning);

                if (GUILayout.Button($"Install {_selectedIde} Rules", GUILayout.Height(28)))
                {
                    InstallRules(_selectedIde);
                    AppendLog($"{_selectedIde} rules installed.");
                }
            }
        }

        private static string GetDestFileName(IdeType ide)
        {
            return ide switch
            {
                IdeType.Cursor => ".cursorrules",
                IdeType.Windsurf => ".windsurfrules",
                IdeType.ClaudeDesktop => ".claude/rules/unity-ui-mcp.md",
                _ => ""
            };
        }

        private static string GetRulesDestFile(IdeType ide)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, GetDestFileName(ide));
        }

        public static string GetRulesSourceFile()
        {
            var guids = AssetDatabase.FindAssets("AI_SKILL t:TextAsset", new[] { "Assets/UnityMCP" });
            if (guids.Length > 0)
                return Path.GetFullPath(AssetDatabase.GUIDToAssetPath(guids[0]));

            var direct = Path.Combine(Application.dataPath, "UnityMCP", "AI_SKILL.md");
            return File.Exists(direct) ? direct : null;
        }

        private static bool CheckRulesInstalled(IdeType ide)
        {
            return File.Exists(GetRulesDestFile(ide));
        }

        private static void InstallRules(IdeType ide)
        {
            var src = GetRulesSourceFile();
            if (src == null || !File.Exists(src))
            {
                Debug.LogError("[UnityMCP] Cannot find AI_SKILL.md in Assets/UnityMCP/");
                return;
            }

            var dest = GetRulesDestFile(ide);
            var destDir = Path.GetDirectoryName(dest);
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            File.Copy(src, dest, overwrite: true);
            Debug.Log($"[UnityMCP] Rules installed -> {dest}");
        }

        private static void RemoveRules(IdeType ide)
        {
            var dest = GetRulesDestFile(ide);
            if (File.Exists(dest))
            {
                File.Delete(dest);
                Debug.Log($"[UnityMCP] Rules removed: {dest}");
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
            // Don't auto-stop when closing window — server keeps running
        }
    }
}
