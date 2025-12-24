using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ClickUpDesktopPowerTools.Core;

/// <summary>
/// Result of script execution via Runtime.evaluate.
/// </summary>
public class ScriptExecutionResult
{
    public bool Success { get; set; }
    public string? Value { get; set; }
    public string? ExceptionMessage { get; set; }
}

/// <summary>
/// CDP connection manager for ClickUp Desktop runtime integration.
/// Per RULE.md: Core owns "ClickUp Desktop runtime communication" at platform level.
/// Minimal CDP usage - only essential commands, no abstractions.
/// </summary>
public class RuntimeBridge : IDisposable
{
    private readonly ILogger<RuntimeBridge> _logger;
    private readonly HttpClient _httpClient;
    private readonly ClickUpRuntime _clickUpRuntime;
    private readonly SystemIntegrationSettings _settings;
    private readonly object _lock = new object();
    
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _receiveTask;
    private int _nextMessageId = 1;
    private readonly Dictionary<int, TaskCompletionSource<JsonNode>> _pendingRequests = new();
    
    private RuntimeConnectionState _connectionState = RuntimeConnectionState.Disconnected;
    private int _reconnectAttempts = 0;
    private const int MaxReconnectAttempts = 3;
    private string? _lastKnownUrl;
    
    public event EventHandler<RuntimeConnectionState>? ConnectionStateChanged;
    public event EventHandler<string>? NavigationOccurred;

    public RuntimeConnectionState ConnectionState
    {
        get { lock (_lock) return _connectionState; }
        private set
        {
            lock (_lock)
            {
                if (_connectionState != value)
                {
                    _connectionState = value;
                    ConnectionStateChanged?.Invoke(this, value);
                }
            }
        }
    }

    public string? LastKnownUrl
    {
        get { lock (_lock) return _lastKnownUrl; }
        private set { lock (_lock) _lastKnownUrl = value; }
    }

    public RuntimeBridge(ILogger<RuntimeBridge> logger, ClickUpRuntime clickUpRuntime, SystemIntegrationSettings settings)
    {
        _logger = logger;
        _clickUpRuntime = clickUpRuntime;
        _settings = settings;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    /// <summary>
    /// Connect to ClickUp Desktop via CDP.
    /// </summary>
    public async Task<bool> ConnectAsync(int port = 9222)
    {
        lock (_lock)
        {
            if (_connectionState == RuntimeConnectionState.Connected || 
                _connectionState == RuntimeConnectionState.Connecting)
            {
                _logger.LogDebug("Already connected or connecting");
                return _connectionState == RuntimeConnectionState.Connected;
            }
            
            _reconnectAttempts = 0;
            ConnectionState = RuntimeConnectionState.Connecting;
        }

        return await TryConnectInternalAsync(port);
    }

    /// <summary>
    /// Manual retry - resets failure count and attempts connection.
    /// </summary>
    public async Task<bool> TryConnectAsync(int port = 9222)
    {
        lock (_lock)
        {
            _reconnectAttempts = 0;
            if (_connectionState == RuntimeConnectionState.Connected)
            {
                return true;
            }
            ConnectionState = RuntimeConnectionState.Connecting;
        }

        return await TryConnectInternalAsync(port);
    }

    private async Task<bool> TryConnectInternalAsync(int port)
    {
        try
        {
            // Step 1: Get target list
            var targets = await GetTargetsAsync(port);
            if (targets == null || targets.Count == 0)
            {
                _logger.LogWarning("No CDP targets found on port {Port}", port);
                ConnectionState = RuntimeConnectionState.Failed;
                return false;
            }

            // Step 2: Select target (prefer page type with clickup.com URL)
            var target = SelectTarget(targets);
            if (target == null)
            {
                _logger.LogWarning("No suitable CDP target found");
                ConnectionState = RuntimeConnectionState.Failed;
                return false;
            }

            var wsUrl = target["webSocketDebuggerUrl"]?.GetValue<string>();
            if (string.IsNullOrEmpty(wsUrl))
            {
                _logger.LogWarning("Target has no webSocketDebuggerUrl");
                ConnectionState = RuntimeConnectionState.Failed;
                return false;
            }

            // Step 3: Connect WebSocket
            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();
            
            await _webSocket.ConnectAsync(new Uri(wsUrl), _cancellationTokenSource.Token);
            
            _logger.LogInformation("Connected to CDP target: {Url}", target["url"]?.GetValue<string>());

            // Step 4: Start receiving messages
            _receiveTask = Task.Run(() => ReceiveLoopAsync(_cancellationTokenSource.Token));

            // Step 5: Initialize CDP domains
            await SendCommandAsync("Runtime.enable", new JsonObject());
            await SendCommandAsync("Page.enable", new JsonObject());

            // Step 6: Subscribe to navigation events
            await SendCommandAsync("Page.addScriptToEvaluateOnNewDocument", new JsonObject
            {
                ["source"] = GetRuntimeHelpersScript()
            });

            // Step 7: Inject helpers into current page
            await ExecuteScriptAsync(GetRuntimeHelpersScript());

            // Step 8: Get initial URL
            var url = await ExecuteScriptAsync("window.location.href");
            if (!string.IsNullOrEmpty(url))
            {
                LastKnownUrl = url;
            }

            ConnectionState = RuntimeConnectionState.Connected;
            _reconnectAttempts = 0;
            
            _logger.LogInformation("Runtime bridge connected successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to CDP");
            ConnectionState = RuntimeConnectionState.Failed;
            await DisconnectInternalAsync();
            return false;
        }
    }

    private async Task<List<JsonNode>?> GetTargetsAsync(int port)
    {
        try
        {
            // Try /json/list first (newer CDP), fall back to /json (older)
            var urls = new[] { $"/json/list", "/json" };
            
            foreach (var endpoint in urls)
            {
                try
                {
                    var url = $"http://localhost:{port}{endpoint}";
                    var response = await _httpClient.GetStringAsync(url);
                    var targets = JsonNode.Parse(response) as JsonArray;
                    
                    if (targets != null && targets.Count > 0)
                    {
                        var result = new List<JsonNode>();
                        foreach (var target in targets)
                        {
                            if (target != null) result.Add(target);
                        }
                        return result;
                    }
                }
                catch (HttpRequestException)
                {
                    // Try next endpoint
                    continue;
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get CDP targets");
            return null;
        }
    }

    private JsonNode? SelectTarget(List<JsonNode> targets)
    {
        // Filter: type == "page" AND url contains "clickup.com"
        var pageTargets = new List<(JsonNode target, string url)>();
        
        foreach (var target in targets)
        {
            var type = target["type"]?.GetValue<string>();
            var url = target["url"]?.GetValue<string>() ?? "";
            
            if (type == "page" && url.Contains("clickup.com", StringComparison.OrdinalIgnoreCase))
            {
                pageTargets.Add((target, url));
            }
        }

        if (pageTargets.Count == 0)
        {
            // Fallback: any page target
            foreach (var target in targets)
            {
                var type = target["type"]?.GetValue<string>();
                if (type == "page")
                {
                    return target;
                }
            }
            return null;
        }

        // Prefer shortest URL (likely main window)
        pageTargets.Sort((a, b) => a.url.Length.CompareTo(b.url.Length));
        return pageTargets[0].target;
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        
        try
        {
            while (!cancellationToken.IsCancellationRequested && _webSocket?.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), cancellationToken);
                
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("WebSocket closed by remote");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleMessageAsync(message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in receive loop");
        }
        finally
        {
            // Handle disconnect
            if (ConnectionState == RuntimeConnectionState.Connected)
            {
                await HandleDisconnectAsync();
            }
        }
    }

    private Task HandleMessageAsync(string messageJson)
    {
        try
        {
            var message = JsonNode.Parse(messageJson);
            if (message == null) return Task.CompletedTask;

            // Check if it's a response to a command
            var idNode = message["id"];
            if (idNode != null)
            {
                var id = idNode.GetValue<int>();
                if (id > 0)
                {
                    lock (_lock)
                    {
                        if (_pendingRequests.TryGetValue(id, out var tcs))
                        {
                            _pendingRequests.Remove(id);
                            tcs.SetResult(message);
                        }
                    }
                    return Task.CompletedTask;
                }
            }

            // Check if it's an event
            var method = message["method"]?.GetValue<string>();
            if (method == "Page.frameNavigated")
            {
                var url = message["params"]?["frame"]?["url"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(url))
                {
                    LastKnownUrl = url;
                    NavigationOccurred?.Invoke(this, url);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to handle CDP message");
        }
        
        return Task.CompletedTask;
    }

    private async Task HandleDisconnectAsync()
    {
        ConnectionState = RuntimeConnectionState.Disconnected;
        
        // Check if ClickUp process is still running
        var status = _clickUpRuntime.CheckStatus();
        if (status != ClickUpDesktopStatus.Running)
        {
            ConnectionState = RuntimeConnectionState.Failed;
            _logger.LogInformation("ClickUp process not running, stopping reconnection attempts");
            return;
        }

        // Try to reconnect
        lock (_lock)
        {
            if (_reconnectAttempts < MaxReconnectAttempts)
            {
                _reconnectAttempts++;
            }
            else
            {
                ConnectionState = RuntimeConnectionState.Failed;
                _logger.LogWarning("Max reconnect attempts reached");
                return;
            }
        }

        // Exponential backoff: 1s, 2s, 4s
        var delay = TimeSpan.FromSeconds(Math.Pow(2, _reconnectAttempts - 1));
        _logger.LogInformation("Reconnecting in {Delay} seconds (attempt {Attempt}/{Max})", 
            delay.TotalSeconds, _reconnectAttempts, MaxReconnectAttempts);
        
        await Task.Delay(delay);
        
        // Check process status again before reconnecting
        status = _clickUpRuntime.CheckStatus();
        if (status != ClickUpDesktopStatus.Running)
        {
            ConnectionState = RuntimeConnectionState.Failed;
            _logger.LogInformation("ClickUp process stopped during reconnect delay");
            return;
        }
        
        // Try to reconnect using port from settings
        await TryConnectInternalAsync(_settings.DebugPort);
    }

    /// <summary>
    /// Execute JavaScript in the runtime context with structured result.
    /// Checks for exceptionDetails to determine success, not just return value.
    /// </summary>
    public async Task<ScriptExecutionResult> ExecuteScriptWithResultAsync(string js)
    {
        if (ConnectionState != RuntimeConnectionState.Connected || _webSocket == null)
        {
            return new ScriptExecutionResult
            {
                Success = false,
                ExceptionMessage = "Runtime not connected"
            };
        }

        try
        {
            var result = await SendCommandAsync("Runtime.evaluate", new JsonObject
            {
                ["expression"] = js,
                ["returnByValue"] = true
            });

            if (result == null)
            {
                return new ScriptExecutionResult
                {
                    Success = false,
                    ExceptionMessage = "No response from runtime"
                };
            }

            // Check for exceptionDetails first - this indicates an error
            var exceptionDetails = result["result"]?["exceptionDetails"];
            if (exceptionDetails != null)
            {
                var exceptionText = exceptionDetails["text"]?.GetValue<string>() ?? 
                                   exceptionDetails["exception"]?["description"]?.GetValue<string>() ??
                                   exceptionDetails["exception"]?["value"]?.GetValue<string>() ??
                                   "Unknown exception";
                return new ScriptExecutionResult
                {
                    Success = false,
                    ExceptionMessage = exceptionText
                };
            }

            // No exception means success, even if value is null/undefined
            // CDP returns values differently based on type:
            // - Strings are returned as-is
            // - Objects/arrays are returned as JSON strings when returnByValue=true
            // - null/undefined are returned as null
            
            // Extract the result object from the CDP response
            // CDP response structure: {"id": 1, "result": {"type": "string", "value": "..."}}
            var resultNode = result?["result"];
            string? value = null;
            
            if (resultNode != null)
            {
                // Try different ways to access the value
                JsonNode? valueNode = null;
                string? type = null;
                
                // Method 1: Direct property access on resultNode (which should be the inner result object)
                if (resultNode is System.Text.Json.Nodes.JsonObject jsonObj)
                {
                    if (jsonObj.TryGetPropertyValue("value", out var directValueNode))
                    {
                        valueNode = directValueNode;
                    }
                    if (jsonObj.TryGetPropertyValue("type", out var typeNode))
                    {
                        type = typeNode?.GetValue<string>();
                    }
                }
                
                // Method 2: Indexer access (fallback)
                if (valueNode == null)
                {
                    valueNode = resultNode["value"];
                }
                if (string.IsNullOrEmpty(type))
                {
                    type = resultNode["type"]?.GetValue<string>();
                }
                
                // Method 3: If resultNode still has nested "result", extract it
                if (valueNode == null && resultNode is System.Text.Json.Nodes.JsonObject jsonObjNested)
                {
                    if (jsonObjNested.TryGetPropertyValue("result", out var nestedResultNode))
                    {
                        // The resultNode itself contains another "result" property - extract from that
                        if (nestedResultNode is System.Text.Json.Nodes.JsonObject nestedResultObj)
                        {
                            if (nestedResultObj.TryGetPropertyValue("value", out var nestedValueNode))
                            {
                                valueNode = nestedValueNode;
                            }
                            if (nestedResultObj.TryGetPropertyValue("type", out var nestedTypeNode))
                            {
                                type = nestedTypeNode?.GetValue<string>();
                            }
                        }
                    }
                }
                
                if (valueNode != null)
                {
                    // Try to get as string first (for JSON strings)
                    if (valueNode.GetValueKind() == System.Text.Json.JsonValueKind.String)
                    {
                        value = valueNode.GetValue<string>();
                    }
                    // If it's an object/array, serialize it to JSON string
                    else if (valueNode.GetValueKind() == System.Text.Json.JsonValueKind.Object || 
                             valueNode.GetValueKind() == System.Text.Json.JsonValueKind.Array)
                    {
                        value = valueNode.ToJsonString();
                    }
                    // For null/undefined, value stays null
                }
                else if (type == "string" && resultNode is System.Text.Json.Nodes.JsonObject jsonObjStringFallback && jsonObjStringFallback.TryGetPropertyValue("value", out var stringValueNode))
                {
                    // Sometimes CDP returns strings differently
                    value = stringValueNode?.GetValue<string>();
                }
            }
            
            return new ScriptExecutionResult
            {
                Success = true,
                Value = value
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Script execution failed");
            return new ScriptExecutionResult
            {
                Success = false,
                ExceptionMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Execute JavaScript in the runtime context.
    /// Returns null on error or if result is not a string.
    /// Legacy method for backward compatibility.
    /// </summary>
    public async Task<string?> ExecuteScriptAsync(string js)
    {
        var result = await ExecuteScriptWithResultAsync(js);
        return result.Success ? result.Value : null;
    }

    private async Task<JsonNode?> SendCommandAsync(string method, JsonObject parameters)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            return null;
        }

        int id;
        TaskCompletionSource<JsonNode> tcs;
        
        lock (_lock)
        {
            id = _nextMessageId++;
            tcs = new TaskCompletionSource<JsonNode>();
            _pendingRequests[id] = tcs;
        }

        try
        {
            var message = new JsonObject
            {
                ["id"] = id,
                ["method"] = method,
                ["params"] = parameters
            };

            var json = message.ToJsonString();
            var bytes = Encoding.UTF8.GetBytes(json);
            
            await _webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                _cancellationTokenSource?.Token ?? CancellationToken.None);

            // Wait for response with timeout
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                timeoutCts.Token,
                _cancellationTokenSource?.Token ?? CancellationToken.None);
            
            var response = await tcs.Task.WaitAsync(linkedCts.Token);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CDP command failed: {Method}", method);
            lock (_lock)
            {
                _pendingRequests.Remove(id);
            }
            return null;
        }
    }

    private string GetRuntimeHelpersScript()
    {
        return @"
(function() {
    if (window.__clickupPowerToolsHelpers) return;
    window.__clickupPowerToolsHelpers = true;
    
    window.getTaskIdFromUrl = function() {
        try {
            const url = window.location?.href;
            if (!url) return null;
            const match = url.match(/\/t\/([a-zA-Z0-9]+)/);
            return match ? match[1] : null;
        } catch {
            return null;
        }
    };
})();
";
    }

    public async Task DisconnectAsync()
    {
        await DisconnectInternalAsync();
    }

    private async Task DisconnectInternalAsync()
    {
        lock (_lock)
        {
            _cancellationTokenSource?.Cancel();
        }

        if (_receiveTask != null)
        {
            try
            {
                await _receiveTask;
            }
            catch
            {
                // Ignore
            }
        }

        if (_webSocket != null)
        {
            try
            {
                if (_webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closing",
                        CancellationToken.None);
                }
            }
            catch
            {
                // Ignore
            }
            finally
            {
                _webSocket.Dispose();
                _webSocket = null;
            }
        }

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        lock (_lock)
        {
            _pendingRequests.Clear();
        }

        ConnectionState = RuntimeConnectionState.Disconnected;
    }

    public void Dispose()
    {
        _ = DisconnectInternalAsync();
        _httpClient?.Dispose();
    }
}

