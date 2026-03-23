using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UnityMCP.Core
{
    /// <summary>
    /// MCP Server chạy trực tiếp trong Unity Editor.
    /// Hỗ trợ SSE transport (GET /sse) + message endpoint (POST /messages).
    /// Claude Desktop / Claude Code kết nối trực tiếp, không cần Python.
    /// </summary>
    public class McpHttpServer
    {
        public const int Port = 7890;
        public const string MCP_PROTOCOL_VERSION = "2024-11-05";

        private HttpListener _http;
        private CancellationTokenSource _cts;

        private readonly ConcurrentDictionary<string, SseSession> _sessions = new();
        private readonly ConcurrentQueue<PendingRequest> _incoming = new();

        public bool IsRunning { get; private set; }

        private class SseSession
        {
            public string Id;
            public HttpListenerResponse Response;
            public StreamWriter Writer;
            public CancellationTokenSource Cts;
            public bool IsAlive => !Cts.IsCancellationRequested;
        }

        private class PendingRequest
        {
            public string SessionId;
            public Dictionary<string, object> JsonRpc;
        }

        // ── Start / Stop ──────────────────────────────────────────────────

        public void Start()
        {
            if (IsRunning) return;

            _cts = new CancellationTokenSource();
            _http = new HttpListener();
            _http.Prefixes.Add($"http://localhost:{Port}/");
            _http.Start();
            IsRunning = true;

            Task.Run(() => AcceptLoop(_cts.Token));
            EditorApplication.update += ProcessQueue;

            Debug.Log($"[UnityMCP] MCP Server started on http://localhost:{Port}");
            Debug.Log($"[UnityMCP] SSE endpoint: http://localhost:{Port}/sse");
        }

        public void Stop()
        {
            if (!IsRunning) return;

            EditorApplication.update -= ProcessQueue;
            _cts?.Cancel();

            foreach (var kvp in _sessions)
            {
                try { kvp.Value.Cts.Cancel(); kvp.Value.Response.Close(); } catch { }
            }
            _sessions.Clear();

            _http?.Stop();
            _http?.Close();
            IsRunning = false;

            Debug.Log("[UnityMCP] MCP Server stopped.");
        }

        // ── Accept loop (background thread) ──────────────────────────────

        private async Task AcceptLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var ctx = await _http.GetContextAsync();
                    _ = Task.Run(() => HandleRequest(ctx, ct));
                }
                catch (Exception) when (ct.IsCancellationRequested) { break; }
                catch (Exception e) { Debug.LogWarning($"[UnityMCP] Accept error: {e.Message}"); }
            }
        }

        private async Task HandleRequest(HttpListenerContext ctx, CancellationToken ct)
        {
            var path = ctx.Request.Url.AbsolutePath;
            var method = ctx.Request.HttpMethod;

            ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            ctx.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            ctx.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (method == "OPTIONS")
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.Close();
                return;
            }

            try
            {
                if (method == "GET" && path == "/sse")
                    await HandleSseConnection(ctx, ct);
                else if (method == "POST" && path.StartsWith("/messages"))
                    await HandleMessage(ctx);
                else
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.Close();
                }
            }
            catch (Exception e) when (!ct.IsCancellationRequested)
            {
                Debug.LogWarning($"[UnityMCP] Request error: {e.Message}");
                try { ctx.Response.StatusCode = 500; ctx.Response.Close(); } catch { }
            }
        }

        // ── SSE Connection ──────────────────────────────────────────────

        private async Task HandleSseConnection(HttpListenerContext ctx, CancellationToken ct)
        {
            var sessionId = Guid.NewGuid().ToString("N");
            var response = ctx.Response;

            response.ContentType = "text/event-stream";
            response.Headers.Add("Cache-Control", "no-cache");
            response.Headers.Add("Connection", "keep-alive");
            response.StatusCode = 200;

            var writer = new StreamWriter(response.OutputStream, new UTF8Encoding(false)) { AutoFlush = true };
            var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            var session = new SseSession
            {
                Id = sessionId,
                Response = response,
                Writer = writer,
                Cts = sessionCts
            };
            _sessions[sessionId] = session;

            Debug.Log($"[UnityMCP] Client connected (session: {sessionId[..8]}...)");

            await WriteSseEvent(writer, "endpoint", $"/messages?sessionId={sessionId}");

            try
            {
                while (!sessionCts.IsCancellationRequested)
                {
                    await Task.Delay(15000, sessionCts.Token);
                    await writer.WriteAsync(": keepalive\n\n");
                    await writer.FlushAsync();
                }
            }
            catch (Exception) { }
            finally
            {
                _sessions.TryRemove(sessionId, out _);
                Debug.Log($"[UnityMCP] Client disconnected (session: {sessionId[..8]}...)");
                try { response.Close(); } catch { }
            }
        }

        // ── Message handling ──────────────────────────────────────────────

        private async Task HandleMessage(HttpListenerContext ctx)
        {
            var query = ctx.Request.Url.Query;
            var sessionId = "";
            if (query.Contains("sessionId="))
            {
                var idx = query.IndexOf("sessionId=") + 10;
                var end = query.IndexOf('&', idx);
                sessionId = end > 0 ? query.Substring(idx, end - idx) : query.Substring(idx);
            }

            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                ctx.Response.StatusCode = 404;
                var errBytes = Encoding.UTF8.GetBytes("{\"error\":\"Session not found\"}");
                ctx.Response.OutputStream.Write(errBytes, 0, errBytes.Length);
                ctx.Response.Close();
                return;
            }

            string body;
            using (var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8))
                body = await reader.ReadToEndAsync();

            var msg = MiniJson.DeserializeObject(body);
            var method = msg.GetString("method");
            var id = msg.ContainsKey("id") ? msg["id"] : null;

            if (method == "initialize")
            {
                var result = BuildInitializeResponse(id);
                await SendSseMessage(session, result);
                ctx.Response.StatusCode = 202;
                ctx.Response.Close();
                return;
            }

            if (method == "notifications/initialized")
            {
                ctx.Response.StatusCode = 202;
                ctx.Response.Close();
                return;
            }

            if (method == "tools/list")
            {
                var result = BuildToolsListResponse(id);
                await SendSseMessage(session, result);
                ctx.Response.StatusCode = 202;
                ctx.Response.Close();
                return;
            }

            if (method == "tools/call")
            {
                _incoming.Enqueue(new PendingRequest
                {
                    SessionId = sessionId,
                    JsonRpc = msg
                });
                ctx.Response.StatusCode = 202;
                ctx.Response.Close();
                return;
            }

            if (method == "ping")
            {
                var pong = new Dictionary<string, object>
                {
                    ["jsonrpc"] = "2.0",
                    ["id"] = id,
                    ["result"] = new Dictionary<string, object>()
                };
                await SendSseMessage(session, pong);
                ctx.Response.StatusCode = 202;
                ctx.Response.Close();
                return;
            }

            // Unknown method
            var error = new Dictionary<string, object>
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["error"] = new Dictionary<string, object>
                {
                    ["code"] = -32601,
                    ["message"] = $"Method not found: {method}"
                }
            };
            await SendSseMessage(session, error);
            ctx.Response.StatusCode = 202;
            ctx.Response.Close();
        }

        // ── MCP Protocol builders ──────────────────────────────────────

        private static Dictionary<string, object> BuildInitializeResponse(object id)
        {
            return new Dictionary<string, object>
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["result"] = new Dictionary<string, object>
                {
                    ["protocolVersion"] = MCP_PROTOCOL_VERSION,
                    ["capabilities"] = new Dictionary<string, object>
                    {
                        ["tools"] = new Dictionary<string, object>
                        {
                            ["listChanged"] = false
                        }
                    },
                    ["serverInfo"] = new Dictionary<string, object>
                    {
                        ["name"] = "unity-ui-mcp",
                        ["version"] = "1.0.0"
                    }
                }
            };
        }

        private static Dictionary<string, object> BuildToolsListResponse(object id)
        {
            var tools = McpToolRegistry.GetToolDefinitions();
            return new Dictionary<string, object>
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["result"] = new Dictionary<string, object>
                {
                    ["tools"] = tools
                }
            };
        }

        // ── Main thread processing ──────────────────────────────────────

        private void ProcessQueue()
        {
            while (_incoming.TryDequeue(out var pending))
            {
                try
                {
                    var paramsObj = pending.JsonRpc.GetObject("params");
                    var toolName = paramsObj.GetString("name");
                    var arguments = paramsObj.GetObject("arguments") ?? new Dictionary<string, object>();
                    var id = pending.JsonRpc.ContainsKey("id") ? pending.JsonRpc["id"] : null;

                    // Build command JSON for dispatcher
                    var cmdDict = new Dictionary<string, object>
                    {
                        ["id"] = id?.ToString() ?? "",
                        ["tool"] = toolName,
                        ["params"] = arguments
                    };
                    var cmdJson = MiniJson.Serialize(cmdDict);

                    var resultStr = CommandDispatcher.Dispatch(cmdJson);
                    var resultObj = MiniJson.DeserializeObject(resultStr);

                    Dictionary<string, object> response;
                    if (resultObj.GetBool("success"))
                    {
                        response = new Dictionary<string, object>
                        {
                            ["jsonrpc"] = "2.0",
                            ["id"] = id,
                            ["result"] = new Dictionary<string, object>
                            {
                                ["content"] = new List<object>
                                {
                                    new Dictionary<string, object>
                                    {
                                        ["type"] = "text",
                                        ["text"] = MiniJson.Serialize(resultObj.ContainsKey("result") ? resultObj["result"] : null, true)
                                    }
                                }
                            }
                        };
                    }
                    else
                    {
                        response = new Dictionary<string, object>
                        {
                            ["jsonrpc"] = "2.0",
                            ["id"] = id,
                            ["result"] = new Dictionary<string, object>
                            {
                                ["content"] = new List<object>
                                {
                                    new Dictionary<string, object>
                                    {
                                        ["type"] = "text",
                                        ["text"] = resultObj.GetString("error", "Unknown error")
                                    }
                                },
                                ["isError"] = true
                            }
                        };
                    }

                    if (_sessions.TryGetValue(pending.SessionId, out var session) && session.IsAlive)
                    {
                        _ = SendSseMessage(session, response);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UnityMCP] Process error: {e}");
                }
            }
        }

        // ── SSE helpers ──────────────────────────────────────────────────

        private static async Task WriteSseEvent(StreamWriter writer, string eventType, string data)
        {
            await writer.WriteAsync($"event: {eventType}\ndata: {data}\n\n");
            await writer.FlushAsync();
        }

        private static async Task SendSseMessage(SseSession session, Dictionary<string, object> jsonRpc)
        {
            try
            {
                var data = MiniJson.Serialize(jsonRpc);
                await WriteSseEvent(session.Writer, "message", data);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UnityMCP] SSE send error: {e.Message}");
            }
        }
    }
}
