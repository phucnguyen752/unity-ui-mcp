using UnityEngine;
using UnityEditor;

namespace UnityMCP
{
    /// <summary>
    /// Stores MCP settings (API key, model choice, etc.) as a ScriptableObject.
    /// Configure via Edit > Project Settings > Unity MCP.
    /// The file is saved to Assets/Editor/McpSettings.asset (gitignored recommended).
    /// </summary>
    public class McpSettings : ScriptableObject
    {
        private const string AssetPath = "Assets/Editor/McpSettings.asset";

        // Singleton access
        private static McpSettings _instance;
        public static McpSettings instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = AssetDatabase.LoadAssetAtPath<McpSettings>(AssetPath);
                if (_instance == null)
                {
                    _instance = CreateInstance<McpSettings>();
                    System.IO.Directory.CreateDirectory("Assets/Editor");
                    AssetDatabase.CreateAsset(_instance, AssetPath);
                    AssetDatabase.SaveAssets();
                }
                return _instance;
            }
        }

        public enum AiProviderType { Claude, OpenAI, Gemini }

        [Tooltip("Select the AI provider to use in the Editor window.")]
        public AiProviderType Provider = AiProviderType.Claude;

        [Tooltip("Your Anthropic Claude API key. Keep this out of version control.")]
        public string ApiKey = "";

        [Tooltip("Claude model to use for vision analysis.")]
        public string Model = "claude-opus-4-6";

        [Tooltip("Your OpenAI API key.")]
        public string OpenAIApiKey = "";

        [Tooltip("OpenAI model to use (e.g. gpt-4o).")]
        public string OpenAIModel = "gpt-4o";

        [Tooltip("Your Gemini API key.")]
        public string GeminiApiKey = "";

        [Tooltip("Gemini model to use (e.g. gemini-1.5-pro).")]
        public string GeminiModel = "gemini-1.5-pro";

        [Tooltip("Default Canvas reference resolution.")]
        public Vector2 DefaultResolution = new(1920, 1080);
    }

    // ── Project Settings provider ─────────────────────────────
    internal static class McpSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider Create() =>
            new SettingsProvider("Project/Unity MCP", SettingsScope.Project)
            {
                label      = "Unity MCP",
                guiHandler = _ =>
                {
                    var settings = McpSettings.instance;
                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.Space(8);
                    EditorGUILayout.LabelField("AI Provider Settings", EditorStyles.boldLabel);

                    settings.Provider = (McpSettings.AiProviderType)EditorGUILayout.EnumPopup("Provider", settings.Provider);

                    EditorGUILayout.Space(4);

                    switch (settings.Provider)
                    {
                        case McpSettings.AiProviderType.Claude:
                            settings.ApiKey = EditorGUILayout.PasswordField("API Key", settings.ApiKey);
                            settings.Model  = EditorGUILayout.TextField("Model",   settings.Model);
                            break;
                        case McpSettings.AiProviderType.OpenAI:
                            settings.OpenAIApiKey = EditorGUILayout.PasswordField("OpenAI API Key", settings.OpenAIApiKey);
                            settings.OpenAIModel  = EditorGUILayout.TextField("Model",   settings.OpenAIModel);
                            break;
                        case McpSettings.AiProviderType.Gemini:
                            settings.GeminiApiKey = EditorGUILayout.PasswordField("Gemini API Key", settings.GeminiApiKey);
                            settings.GeminiModel  = EditorGUILayout.TextField("Model",   settings.GeminiModel);
                            break;
                    }

                    EditorGUILayout.Space(8);
                    EditorGUILayout.LabelField("Defaults", EditorStyles.boldLabel);
                    settings.DefaultResolution = EditorGUILayout.Vector2Field("Reference Resolution", settings.DefaultResolution);

                    EditorGUILayout.Space(8);
                    EditorGUILayout.HelpBox(
                        "Add Assets/Editor/McpSettings.asset to .gitignore to keep your API key private.",
                        MessageType.Warning);

                    if (EditorGUI.EndChangeCheck())
                        EditorUtility.SetDirty(settings);
                }
            };
    }
}
