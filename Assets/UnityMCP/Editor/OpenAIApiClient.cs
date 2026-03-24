using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityMCP.Core;

namespace UnityMCP
{
    public class OpenAIApiClient : IAiApiClient
    {
        private const string ApiUrl = "https://api.openai.com/v1/chat/completions";
        
        private static readonly HttpClient Http = new();

        private readonly List<Dictionary<string, object>> _history = new();

        private string ApiKey => McpSettings.instance.OpenAIApiKey;
        private string Model => McpSettings.instance.OpenAIModel;

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
                                ["type"] = "text",
                                ["text"] = prompt
                            },
                            new Dictionary<string, object>
                            {
                                ["type"] = "image_url",
                                ["image_url"] = new Dictionary<string, object>
                                {
                                    ["url"] = $"data:image/png;base64,{base64}"
                                }
                            }
                        }
                    }
                }
            };

            return await PostAsync(body);
        }

        public async Task<string> Chat(string userMessage)
        {
            // If it's the first message, insert system prompt
            if (_history.Count == 0)
            {
                _history.Add(new Dictionary<string, object>
                {
                    ["role"] = "system",
                    ["content"] = SystemPrompt
                });
            }

            _history.Add(new Dictionary<string, object> { ["role"] = "user", ["content"] = userMessage });

            var body = new Dictionary<string, object>
            {
                ["model"] = Model,
                ["max_tokens"] = 2048,
                ["messages"] = _history
            };

            var reply = await PostAsync(body);
            _history.Add(new Dictionary<string, object> { ["role"] = "assistant", ["content"] = reply });
            return reply;
        }

        private async Task<string> PostAsync(object body)
        {
            if (string.IsNullOrEmpty(ApiKey))
                throw new InvalidOperationException("OpenAI API key not set. Go to Edit > Project Settings > Unity MCP.");

            var json = MiniJson.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
            request.Content = content;

            var response = await Http.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"API error {response.StatusCode}: {raw}");

            var parsed = MiniJson.DeserializeObject(raw);
            var choices = parsed?.GetArray("choices");
            if (choices != null && choices.Count > 0)
            {
                var firstChoice = choices[0] as Dictionary<string, object>;
                var message = firstChoice?.GetObject("message");
                var text = message?.GetString("content");
                if (!string.IsNullOrEmpty(text))
                {
                    // Clean up markdown block if API wrapped the response in ```json ... ```
                    text = text.Trim();
                    if (text.StartsWith("```json"))
                        text = text.Substring(7);
                    else if (text.StartsWith("```"))
                        text = text.Substring(3);
                    if (text.EndsWith("```"))
                        text = text.Substring(0, text.Length - 3);

                    return text.Trim();
                }
            }

            throw new InvalidOperationException("Empty response from OpenAI.");
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
