using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityMCP
{
    public class McpEditorWindow : EditorWindow
    {
        // ── State ──────────────────────────────────────────────
        private Texture2D _fakescreenTexture;
        private string    _chatInput      = "";
        private Vector2   _scrollPos;
        private bool      _isProcessing;

        private readonly List<ChatMessage> _messages = new();

        private IAiApiClient      _apiClient;
        private ToolDispatcher    _dispatcher;

        // ── Resolution presets ────────────────────────────────
        private static readonly string[] ResolutionLabels = { "1920×1080", "1280×720", "375×812 (Mobile)", "Custom" };
        private static readonly Vector2[] Resolutions      = { new(1920,1080), new(1280,720), new(375,812), Vector2.zero };
        private int     _resIndex  = 0;
        private Vector2 _customRes = new(1920, 1080);

        // ── Multiscreen ───────────────────────────────────────
        private bool _generatePortrait   = true;
        private bool _generateLandscape  = true;
        private bool _generateTablet     = false;

        // ── Open ──────────────────────────────────────────────
        [MenuItem("Tools/Unity MCP UI Builder")]
        public static void Open()
        {
            var win = GetWindow<McpEditorWindow>("MCP UI Builder");
            win.minSize = new Vector2(420, 600);
        }

        private void OnEnable()
        {
            var provider = McpSettings.instance.Provider;
            switch (provider)
            {
                case McpSettings.AiProviderType.OpenAI:
                    _apiClient = new OpenAIApiClient();
                    break;
                case McpSettings.AiProviderType.Gemini:
                    _apiClient = new GeminiApiClient();
                    break;
                case McpSettings.AiProviderType.Claude:
                default:
                    _apiClient = new ClaudeApiClient();
                    break;
            }
            _dispatcher = new ToolDispatcher();
        }

        // ── GUI ───────────────────────────────────────────────
        private void OnGUI()
        {
            DrawHeader();
            DrawProviderSelector();
            DrawImageUpload();
            DrawResolutionSelector();
            DrawMultiscreenOptions();
            EditorGUILayout.Space(4);
            DrawChatHistory();
            DrawInputBar();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(8);
            GUILayout.Label("MCP UI Builder", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Upload a fakescreen, then describe any adjustments.", EditorStyles.miniLabel);
            EditorGUILayout.Space(4);
        }

        private void DrawProviderSelector()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("AI Provider:", GUILayout.Width(100));
            var currentProvider = McpSettings.instance.Provider;
            
            EditorGUI.BeginChangeCheck();
            var newProvider = (McpSettings.AiProviderType)EditorGUILayout.EnumPopup(currentProvider);
            if (EditorGUI.EndChangeCheck())
            {
                McpSettings.instance.Provider = newProvider;
                EditorUtility.SetDirty(McpSettings.instance);
                
                // Re-initialize client
                switch (newProvider)
                {
                    case McpSettings.AiProviderType.OpenAI:
                        _apiClient = new OpenAIApiClient();
                        break;
                    case McpSettings.AiProviderType.Gemini:
                        _apiClient = new GeminiApiClient();
                        break;
                    case McpSettings.AiProviderType.Claude:
                    default:
                        _apiClient = new ClaudeApiClient();
                        break;
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
        }

        private void DrawImageUpload()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Fakescreen:", GUILayout.Width(100));

            _fakescreenTexture = (Texture2D)EditorGUILayout.ObjectField(
                _fakescreenTexture, typeof(Texture2D), false, GUILayout.Height(80), GUILayout.Width(80));

            if (_fakescreenTexture != null)
            {
                EditorGUILayout.BeginVertical();
                GUILayout.Label($"{_fakescreenTexture.width}×{_fakescreenTexture.height}px", EditorStyles.miniLabel);
                if (GUILayout.Button("Analyze & Build", GUILayout.Height(30)))
                    _ = AnalyzeAndBuild();
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("Drag a PNG/JPG here.", MessageType.Info);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawResolutionSelector()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Target:", GUILayout.Width(50));
            _resIndex = EditorGUILayout.Popup(_resIndex, ResolutionLabels);
            if (_resIndex == 3)
            {
                _customRes.x = EditorGUILayout.FloatField(_customRes.x, GUILayout.Width(55));
                GUILayout.Label("×", GUILayout.Width(10));
                _customRes.y = EditorGUILayout.FloatField(_customRes.y, GUILayout.Width(55));
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawMultiscreenOptions()
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Variants:", GUILayout.Width(60));
            _generatePortrait  = GUILayout.Toggle(_generatePortrait,  "Portrait",  GUILayout.Width(65));
            _generateLandscape = GUILayout.Toggle(_generateLandscape, "Landscape", GUILayout.Width(75));
            _generateTablet    = GUILayout.Toggle(_generateTablet,    "Tablet",    GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawChatHistory()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));
            foreach (var msg in _messages)
            {
                var style = msg.IsUser ? EditorStyles.helpBox : EditorStyles.wordWrappedLabel;
                EditorGUILayout.LabelField(msg.IsUser ? $"You: {msg.Text}" : $"AI: {msg.Text}", style);
            }
            if (_isProcessing)
                EditorGUILayout.LabelField("Building UI...", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndScrollView();
        }

        private void DrawInputBar()
        {
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !_isProcessing;
            _chatInput = EditorGUILayout.TextField(_chatInput);
            if (GUILayout.Button("Send", GUILayout.Width(60)) && !string.IsNullOrWhiteSpace(_chatInput))
                _ = SendChat(_chatInput);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
        }

        // ── Core flows ────────────────────────────────────────
        private async Task AnalyzeAndBuild()
        {
            if (_fakescreenTexture == null) return;
            _isProcessing = true;
            Repaint();

            try
            {
                var targetRes = _resIndex == 3 ? _customRes : Resolutions[_resIndex];
                var prompt = BuildAnalysisPrompt(targetRes);

                AddMessage("Analyzing fakescreen...", false);
                var json = await _apiClient.AnalyzeImage(_fakescreenTexture, prompt);
                AddMessage("Layout parsed. Building prefab...", false);

                var tree = LayoutParser.Parse(json);
                await _dispatcher.BuildAsync(tree, targetRes);

                // Multiscreen variants
                if (_generatePortrait || _generateLandscape || _generateTablet)
                {
                    var builder = new ScreenVariantBuilder(tree);
                    if (_generatePortrait)   builder.BuildPortrait();
                    if (_generateLandscape)  builder.BuildLandscape();
                    if (_generateTablet)     builder.BuildTablet();
                }

                AddMessage("Done! Prefab created in Hierarchy.", false);
            }
            catch (System.Exception e)
            {
                AddMessage($"Error: {e.Message}", false);
                Debug.LogError($"[MCP] {e}");
            }
            finally
            {
                _isProcessing = false;
                Repaint();
            }
        }

        private async Task SendChat(string text)
        {
            AddMessage(text, true);
            _chatInput   = "";
            _isProcessing = true;
            Repaint();

            try
            {
                var response = await _apiClient.Chat(text);
                AddMessage(response, false);
            }
            catch (System.Exception e)
            {
                AddMessage($"Error: {e.Message}", false);
            }
            finally
            {
                _isProcessing = false;
                Repaint();
            }
        }

        private void AddMessage(string text, bool isUser)
        {
            _messages.Add(new ChatMessage { Text = text, IsUser = isUser });
            _scrollPos.y = float.MaxValue;
            Repaint();
        }

        private string BuildAnalysisPrompt(Vector2 resolution) => $@"
Analyze this UI fakescreen image and return ONLY a JSON object describing the UI layout.
Target resolution: {resolution.x}x{resolution.y}.

JSON schema:
{{
  ""name"": ""RootPanel"",
  ""type"": ""Panel|Button|Image|Text|InputField|ScrollView|Toggle|Slider"",
  ""anchor"": ""top-left|top-center|top-right|middle-left|middle-center|middle-right|bottom-left|bottom-center|bottom-right|stretch-horizontal|stretch-vertical|stretch-full"",
  ""layout"": ""none|horizontal|vertical|grid"",
  ""spacing"": 8,
  ""padding"": {{""left"":8,""right"":8,""top"":8,""bottom"":8}},
  ""size"": {{""width"": 400, ""height"": 300}},
  ""color"": ""#RRGGBBAA"",
  ""text"": ""label text if applicable"",
  ""fontSize"": 16,
  ""fontStyle"": ""normal|bold|italic"",
  ""children"": []
}}

Return only valid JSON, no markdown, no explanation.";
    }

    public class ChatMessage
    {
        public string Text   { get; set; }
        public bool   IsUser { get; set; }
    }
}
