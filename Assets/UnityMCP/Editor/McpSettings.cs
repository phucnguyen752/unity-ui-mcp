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

        [Tooltip("Your Anthropic Claude API key. Keep this out of version control.")]
        public string ApiKey = "";

        [Tooltip("Claude model to use for vision analysis.")]
        public string Model = "claude-opus-4-6";

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
                    EditorGUILayout.LabelField("Claude API", EditorStyles.boldLabel);

                    settings.ApiKey = EditorGUILayout.PasswordField("API Key", settings.ApiKey);
                    settings.Model  = EditorGUILayout.TextField("Model",   settings.Model);

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
