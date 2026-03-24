using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace UnityMCP
{
    /// <summary>
    /// Handles all communication with the Claude API.
    /// Set your API key in Edit > Project Settings > Unity MCP.
    /// </summary>
    public class ClaudeApiClient : IAiApiClient
    {
        private const string ApiUrl = "https://api.anthropic.com/v1/messages";
        private const string Model  = "claude-opus-4-6";

        private static readonly HttpClient Http = new();

        private readonly List<Dictionary<string, object>> _history = new();

        private string ApiKey => McpSettings.instance.ApiKey;

        // ── Analyze image (Vision) ────────────────────────────
        public async Task<string> AnalyzeImage(Texture2D texture, string prompt)
        {
            var base64 = TextureToBase64(texture);

            var body = new Dictionary<string, object>
            {
                ["model"] = Model,
                ["max_tokens"] = 4096,
                ["messages"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["role"] = "user",
                        ["content"] = new List<object>
                        {
                            new Dictionary<string, object>
                            {
                                ["type"] = "image",
                                ["source"] = new Dictionary<string, object>
                                {
                                    ["type"] = "base64",
                                    ["media_type"] = "image/png",
                                    ["data"] = base64
                                }
                            },
                            new Dictionary<string, object>
                            {
                                ["type"] = "text",
                                ["text"] = prompt
                            }
                        }
                    }
                }
            };

            return await PostAsync(body);
        }

        // ── Multi-turn chat ───────────────────────────────────
        public async Task<string> Chat(string userMessage)
        {
            _history.Add(new Dictionary<string, object> { ["role"] = "user", ["content"] = userMessage });

            var body = new Dictionary<string, object>
            {
                ["model"] = Model,
                ["max_tokens"] = 2048,
                ["system"] = SystemPrompt,
                ["messages"] = _history
            };

            var reply = await PostAsync(body);
            _history.Add(new Dictionary<string, object> { ["role"] = "assistant", ["content"] = reply });
            return reply;
        }

        // ── HTTP helper ───────────────────────────────────────
        private async Task<string> PostAsync(object body)
        {
            if (string.IsNullOrEmpty(ApiKey))
                throw new InvalidOperationException("Claude API key not set. Go to Edit > Project Settings > Unity MCP.");

            var json    = MiniJson.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            request.Headers.Add("x-api-key",         ApiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = content;

            var response = await Http.SendAsync(request);
            var raw      = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"API error {response.StatusCode}: {raw}");

            // Parse response: { "content": [ { "type": "text", "text": "..." } ] }
            var parsed = MiniJson.DeserializeObject(raw);
            var contentArr = parsed?.GetArray("content");
            if (contentArr != null && contentArr.Count > 0)
            {
                var first = contentArr[0] as Dictionary<string, object>;
                var text = first?.GetString("text");
                if (!string.IsNullOrEmpty(text)) return text;
            }

            throw new InvalidOperationException("Empty response from Claude.");
        }

        // ── Helpers ───────────────────────────────────────────
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
