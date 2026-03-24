using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace UnityMCP
{
    public class GeminiApiClient : IAiApiClient
    {
        private static readonly HttpClient Http = new();
        private readonly List<Dictionary<string, object>> _history = new();

        private string ApiKey => McpSettings.instance.GeminiApiKey;
        private string Model => McpSettings.instance.GeminiModel;

        public async Task<string> AnalyzeImage(Texture2D texture, string prompt)
        {
            var base64 = TextureToBase64(texture);

            var body = new Dictionary<string, object>
            {
                ["contents"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["role"] = "user",
                        ["parts"] = new List<object>
                        {
                            new Dictionary<string, object> { ["text"] = prompt },
                            new Dictionary<string, object>
                            {
                                ["inlineData"] = new Dictionary<string, object>
                                {
                                    ["mimeType"] = "image/png",
                                    ["data"] = base64
                                }
                            }
                        }
                    }
                },
                ["systemInstruction"] = new Dictionary<string, object>
                {
                    ["parts"] = new List<object> { new Dictionary<string, object> { ["text"] = SystemPrompt } }
                }
            };

            return await PostAsync(body);
        }

        public async Task<string> Chat(string userMessage)
        {
            _history.Add(new Dictionary<string, object>
            {
                ["role"] = "user",
                ["parts"] = new List<object> { new Dictionary<string, object> { ["text"] = userMessage } }
            });

            var body = new Dictionary<string, object>
            {
                ["contents"] = _history,
                ["systemInstruction"] = new Dictionary<string, object>
                {
                    ["parts"] = new List<object> { new Dictionary<string, object> { ["text"] = SystemPrompt } }
                }
            };

            var reply = await PostAsync(body);
            
            _history.Add(new Dictionary<string, object>
            {
                ["role"] = "model",
                ["parts"] = new List<object> { new Dictionary<string, object> { ["text"] = reply } }
            });

            return reply;
        }

        private async Task<string> PostAsync(object body)
        {
            if (string.IsNullOrEmpty(ApiKey))
                throw new InvalidOperationException("Gemini API key not set. Go to Edit > Project Settings > Unity MCP.");

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={ApiKey}";
            var json = MiniJson.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await Http.PostAsync(url, content);
            var raw = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"API error {response.StatusCode}: {raw}");

            var parsed = MiniJson.DeserializeObject(raw);
            var candidates = parsed?.GetArray("candidates");
            if (candidates != null && candidates.Count > 0)
            {
                var first = candidates[0] as Dictionary<string, object>;
                var contentObj = first?.GetObject("content");
                var parts = contentObj?.GetArray("parts");
                if (parts != null && parts.Count > 0)
                {
                    var textPart = parts[0] as Dictionary<string, object>;
                    var text = textPart?.GetString("text");
                    if (!string.IsNullOrEmpty(text))
                    {
                        text = text.Trim();
                        // Clean up markdown code block wrapper if present
                        if (text.StartsWith("```json"))
                            text = text.Substring(7);
                        else if (text.StartsWith("```"))
                            text = text.Substring(3);
                        if (text.EndsWith("```"))
                            text = text.Substring(0, text.Length - 3);

                        return text.Trim();
                    }
                }
            }

            throw new InvalidOperationException("Empty response from Gemini.");
        }

        private static string TextureToBase64(Texture2D tex)
        {
            var path = AssetDatabase.GetAssetPath(tex);
            if (!string.IsNullOrEmpty(path))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (!importer.isReadable)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                }
            }
            return Convert.ToBase64String(tex.EncodeToPNG());
        }

        private const string SystemPrompt = @"
You are a Unity UI expert assistant helping to build uGUI layouts.
When asked to modify a UI, respond with a JSON patch describing the changes.
Keep responses concise and actionable.";
    }
}
