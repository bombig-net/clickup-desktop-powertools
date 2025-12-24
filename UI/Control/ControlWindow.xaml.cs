using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using ClickUpDesktopPowerTools.Core;

namespace ClickUpDesktopPowerTools.UI.Control;

public partial class ControlWindow : Window
{
    // P/Invoke declarations for removing system border
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_CLIENTEDGE = 0x0200;
    private const int WS_EX_STATICEDGE = 0x20000;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_FRAMECHANGED = 0x0020;

    private readonly TokenStorage _tokenStorage;
    private readonly ClickUpApi _clickUpApi;
    private readonly CoreState _coreState;
    private readonly ClickUpRuntime _clickUpRuntime;
    private readonly SystemIntegration _systemIntegration;
    private readonly SystemIntegrationSettings _systemIntegrationSettings;
    private readonly RuntimeBridge? _runtimeBridge;
    private readonly ToolManager? _toolManager;
    private readonly ILogger<ControlWindow> _logger;
    private readonly ToolActivationSettings _toolActivation;

    private bool _allowClose = false;
    private bool _isInitialized = false;
    private bool _webViewFailed = false;
    private bool _hasAttemptedReload = false;
    private DateTime _lastClickTime = DateTime.MinValue;

    public ControlWindow(TokenStorage tokenStorage, ClickUpApi clickUpApi, CoreState coreState, ClickUpRuntime clickUpRuntime, SystemIntegration systemIntegration, SystemIntegrationSettings systemIntegrationSettings, RuntimeBridge? runtimeBridge, ToolManager? toolManager, ILoggerFactory loggerFactory)
    {
        // #region agent log
        try { System.IO.File.AppendAllText(@"c:\dev\clickup-desktop-powertools\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A", location = "ControlWindow.xaml.cs:37", message = "Constructor entry", data = new { }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        
        _tokenStorage = tokenStorage;
        _clickUpApi = clickUpApi;
        _coreState = coreState;
        _clickUpRuntime = clickUpRuntime;
        _systemIntegration = systemIntegration;
        _systemIntegrationSettings = systemIntegrationSettings;
        _runtimeBridge = runtimeBridge;
        _toolManager = toolManager;
        _logger = loggerFactory.CreateLogger<ControlWindow>();
        _toolActivation = SettingsManager.Load<ToolActivationSettings>("ToolActivation");

        // #region agent log
        try { System.IO.File.AppendAllText(@"c:\dev\clickup-desktop-powertools\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A", location = "ControlWindow.xaml.cs:50", message = "Before InitializeComponent", data = new { }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        
        try
        {
            InitializeComponent();
            
            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\dev\clickup-desktop-powertools\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A", location = "ControlWindow.xaml.cs:56", message = "After InitializeComponent", data = new { }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
        }
        catch (Exception ex)
        {
            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\dev\clickup-desktop-powertools\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A", location = "ControlWindow.xaml.cs:62", message = "InitializeComponent exception", data = new { exceptionType = ex.GetType().Name, message = ex.Message, stackTrace = ex.StackTrace }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
            throw;
        }
        
        Loaded += OnLoaded;
        StateChanged += OnStateChanged;
        SourceInitialized += OnSourceInitialized;
        
        // #region agent log
        try { System.IO.File.AppendAllText(@"c:\dev\clickup-desktop-powertools\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A", location = "ControlWindow.xaml.cs:68", message = "Constructor exit", data = new { }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        // Remove system border by modifying window extended style
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            // Remove WS_EX_CLIENTEDGE and WS_EX_STATICEDGE to remove borders
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            extendedStyle &= ~(WS_EX_CLIENTEDGE | WS_EX_STATICEDGE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle);
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        try
        {
            // Set UserDataFolder before initialization to avoid default location issues
            WebView.CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ClickUpDesktopPowerTools",
                    "WebView2Data")
            };

            await WebView.EnsureCoreWebView2Async();

            // Capture WebView2 version after initialization
            try
            {
                _coreState.WebView2Version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get WebView2 version");
            }

            // Subscribe to events after initialization
            WebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            WebView.CoreWebView2.ProcessFailed += OnProcessFailed;
            WebView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

            // Get the wwwroot folder path (relative to executable)
            var exeDirectory = AppContext.BaseDirectory;
            var wwwrootPath = Path.Combine(exeDirectory, "wwwroot");

            WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "powertools.local",
                wwwrootPath,
                CoreWebView2HostResourceAccessKind.Allow);

            WebView.CoreWebView2.Navigate("https://powertools.local/index.html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize WebView2");
            ShowErrorOverlay("Failed to initialize WebView2.\n\nPlease ensure the WebView2 Runtime is installed.");
        }
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        _logger.LogInformation("Navigation completed: Success={Success}, WebViewFailed={Failed}, HttpStatusCode={StatusCode}", 
            e.IsSuccess, _webViewFailed, e.HttpStatusCode);
        
        if (e.IsSuccess && !_webViewFailed)
        {
            // Push initial state to WebUI
            PushState();
        }
        else
        {
            _logger.LogWarning("Navigation failed or WebView crashed. Success={Success}, Failed={Failed}", 
                e.IsSuccess, _webViewFailed);
        }
    }

    private void OnProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        _logger.LogError("WebView2 process failed: {Kind}, Reason: {Reason}", e.ProcessFailedKind, e.Reason);

        switch (e.ProcessFailedKind)
        {
            case CoreWebView2ProcessFailedKind.BrowserProcessExited:
                // Catastrophic failure - WebView2 is unusable
                _webViewFailed = true;
                ShowErrorOverlay("The browser process has crashed.\n\nClose and reopen this window to recover.");
                break;

            case CoreWebView2ProcessFailedKind.RenderProcessExited:
            case CoreWebView2ProcessFailedKind.RenderProcessUnresponsive:
                // Try a single reload, then give up
                if (!_hasAttemptedReload)
                {
                    _hasAttemptedReload = true;
                    _logger.LogWarning("Attempting to reload after render process failure");
                    try
                    {
                        WebView.CoreWebView2.Reload();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to reload after render process failure");
                        _webViewFailed = true;
                        ShowErrorOverlay("The browser process has crashed.\n\nClose and reopen this window to recover.");
                    }
                }
                else
                {
                    _webViewFailed = true;
                    ShowErrorOverlay("The browser process has crashed.\n\nClose and reopen this window to recover.");
                }
                break;

            default:
                _logger.LogWarning("WebView2 process failure (non-critical): {Kind}", e.ProcessFailedKind);
                break;
        }
    }

    private void ShowErrorOverlay(string message)
    {
        // Hide WebView and show error
        WebView.Visibility = Visibility.Collapsed;
        ErrorOverlay.Visibility = Visibility.Visible;
        ErrorMessage.Text = message;
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var json = e.WebMessageAsJson;
            var message = JsonNode.Parse(json);
            if (message == null)
            {
                _logger.LogWarning("Received null message from WebUI");
                return;
            }

            var messageType = message["type"]?.GetValue<string>();
            if (string.IsNullOrEmpty(messageType))
            {
                _logger.LogWarning("Received message without type from WebUI");
                return;
            }

            _logger.LogDebug("Received message from WebUI: {Type}", messageType);

            switch (messageType)
            {
                case "get-state":
                    PushState();
                    break;

                case "set-api-token":
                    HandleSetApiToken(message["payload"]);
                    break;

                case "clear-api-token":
                    HandleClearApiToken();
                    break;

                case "test-api-token":
                    _ = HandleTestApiTokenAsync();
                    break;

                case "set-tool-enabled":
                    HandleSetToolEnabled(message["payload"]);
                    break;

                case "refresh-runtime-status":
                    HandleRefreshRuntimeStatus();
                    break;

                case "open-log-folder":
                    HandleOpenLogFolder();
                    break;

                case "launch-clickup-debug":
                    HandleLaunchClickUpDebug();
                    break;

                case "set-autostart":
                    HandleSetAutostart(message["payload"]);
                    break;

                case "open-clickup-location":
                    HandleOpenClickUpLocation();
                    break;

                case "refresh-clickup-path":
                    HandleRefreshClickUpPath();
                    break;

                case "set-clickup-path-override":
                    HandleSetClickUpPathOverride(message["payload"]);
                    break;

                case "set-debug-port":
                    HandleSetDebugPort(message["payload"]);
                    break;

                case "set-restart-if-running":
                    HandleSetRestartIfRunning(message["payload"]);
                    break;

                case "get-custom-css-js":
                    HandleGetCustomCssJs();
                    break;

                case "set-custom-css-js":
                    HandleSetCustomCssJs(message["payload"]);
                    break;

                case "get-debug-inspector-state":
                    HandleGetDebugInspectorState();
                    break;

                default:
                    _logger.LogWarning("Unknown message type from WebUI: {Type}", messageType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from WebUI");
        }
    }

    private void HandleSetApiToken(JsonNode? payload)
    {
        var token = payload?["token"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("set-api-token received with empty token");
            return;
        }

        _tokenStorage.StoreToken(token);
        _coreState.HasApiToken = true;
        _coreState.TokenValid = null; // Reset validation status
        _logger.LogInformation("API token stored");
        PushState();
    }

    private void HandleClearApiToken()
    {
        _tokenStorage.ClearToken();
        _coreState.HasApiToken = false;
        _coreState.TokenValid = null;
        _logger.LogInformation("API token cleared");
        PushState();
    }

    private async System.Threading.Tasks.Task HandleTestApiTokenAsync()
    {
        if (!_coreState.HasApiToken)
        {
            SendMessage("test-result", new { success = false, error = "No API token configured" });
            return;
        }

        try
        {
            // Test the token by calling the /user endpoint
            var result = await _clickUpApi.GetAsync<JsonNode>("/user");
            _coreState.TokenValid = true;
            _logger.LogInformation("API token validated successfully");
            SendMessage("test-result", new { success = true, error = (string?)null });
        }
        catch (Exception ex)
        {
            _coreState.TokenValid = false;
            _logger.LogWarning(ex, "API token validation failed");
            SendMessage("test-result", new { success = false, error = ex.Message });
        }

        PushState();
    }

    private void HandleSetToolEnabled(JsonNode? payload)
    {
        var toolId = payload?["toolId"]?.GetValue<string>();
        var enabled = payload?["enabled"]?.GetValue<bool>() ?? false;

        if (string.IsNullOrWhiteSpace(toolId))
        {
            _logger.LogWarning("set-tool-enabled received without toolId");
            return;
        }

        _toolActivation.Enabled[toolId] = enabled;
        SettingsManager.Save("ToolActivation", _toolActivation);
        
        // Update CoreState
        _coreState.ActiveTools[toolId] = enabled;
        
        // Notify ToolManager
        _toolManager?.OnToolActivationChanged(toolId, enabled);
        
        _logger.LogInformation("Tool {ToolId} enabled: {Enabled}", toolId, enabled);
        PushState();
    }

    private void HandleOpenLogFolder()
    {
        try
        {
            var logDir = Path.GetDirectoryName(_coreState.LogFilePath);
            if (!string.IsNullOrEmpty(logDir) && Directory.Exists(logDir))
            {
                Process.Start("explorer.exe", logDir);
            }
            else
            {
                _logger.LogWarning("Log directory does not exist: {LogDir}", logDir);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open log folder");
        }
    }

    private async void HandleRefreshRuntimeStatus()
    {
        _coreState.ClickUpDesktopStatus = _clickUpRuntime.CheckStatus();
        _coreState.ClickUpDebugPortAvailable = await _clickUpRuntime.CheckDebugPortAvailability(_systemIntegrationSettings.DebugPort);
        
        // If runtime bridge exists and ClickUp is running, try to connect
        if (_runtimeBridge != null && _coreState.ClickUpDesktopStatus == ClickUpDesktopStatus.Running)
        {
            _ = Task.Run(async () =>
            {
                var connected = await _runtimeBridge.TryConnectAsync(_systemIntegrationSettings.DebugPort);
                if (connected && _toolManager != null)
                {
                    var ctx = new RuntimeContext(_runtimeBridge);
                    _toolManager.OnRuntimeConnected(ctx);
                }
            });
        }
        
        PushState();
    }

    private void HandleLaunchClickUpDebug()
    {
        var (success, error) = _systemIntegration.LaunchClickUpDebugMode(
            _coreState.ClickUpInstallPath,
            _systemIntegrationSettings);
        SendMessage("launch-result", new { success, error });
        
        // Refresh runtime status after short delay (process needs time to start)
        if (success)
        {
            _ = System.Threading.Tasks.Task.Delay(2000).ContinueWith(async _ =>
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    _coreState.ClickUpDesktopStatus = _clickUpRuntime.CheckStatus();
                    _coreState.ClickUpDebugPortAvailable = await _clickUpRuntime.CheckDebugPortAvailability(_systemIntegrationSettings.DebugPort);
                    PushState();
                });
            });
        }
    }

    private void HandleSetAutostart(JsonNode? payload)
    {
        var enabled = payload?["enabled"]?.GetValue<bool>() ?? false;
        if (_systemIntegration.SetAutostartEnabled(enabled))
        {
            _coreState.AutostartEnabled = enabled;
        }
        PushState();
    }

    private void HandleOpenClickUpLocation()
    {
        _systemIntegration.OpenFolder(_coreState.ClickUpInstallPath);
    }

    private void HandleRefreshClickUpPath()
    {
        _coreState.ClickUpInstallPath = _systemIntegration.ResolveClickUpInstallPath(_systemIntegrationSettings);
        PushState();
    }

    private void HandleSetClickUpPathOverride(JsonNode? payload)
    {
        var path = payload?["path"]?.GetValue<string>();
        _systemIntegrationSettings.ClickUpInstallPathOverride = string.IsNullOrWhiteSpace(path) ? null : path;
        _systemIntegrationSettings.Save();
        
        // Re-resolve path
        _coreState.ClickUpInstallPath = _systemIntegration.ResolveClickUpInstallPath(_systemIntegrationSettings);
        PushState();
    }

    private void HandleSetDebugPort(JsonNode? payload)
    {
        var port = payload?["port"]?.GetValue<int>() ?? 9222;
        if (port >= 1024 && port <= 65535)
        {
            _systemIntegrationSettings.DebugPort = port;
            _systemIntegrationSettings.Save();
            PushState();
        }
    }

    private void HandleSetRestartIfRunning(JsonNode? payload)
    {
        var enabled = payload?["enabled"]?.GetValue<bool>() ?? false;
        _systemIntegrationSettings.RestartIfRunning = enabled;
        _systemIntegrationSettings.Save();
        PushState();
    }

    private void HandleGetCustomCssJs()
    {
        // Check if tool is enabled
        if (!_toolActivation.Enabled.GetValueOrDefault("custom-css-js", false))
        {
            // Tool disabled - return empty state
            SendMessage("custom-css-js-state", new
            {
                css = string.Empty,
                js = string.Empty,
                lastResult = (string?)null,
                lastError = (string?)null
            });
            return;
        }

        var tool = _toolManager?.GetToolInstance("custom-css-js") as Tools.CustomCssJs.CustomCssJsTool;
        if (tool != null)
        {
            var (css, js) = tool.GetSettings();
            SendMessage("custom-css-js-state", new
            {
                css = css ?? string.Empty,
                js = js ?? string.Empty,
                lastResult = tool.LastInjectionResult,
                lastError = tool.LastInjectionError
            });
        }
        else
        {
            SendMessage("custom-css-js-state", new
            {
                css = string.Empty,
                js = string.Empty,
                lastResult = (string?)null,
                lastError = (string?)null
            });
        }
    }

    private void HandleSetCustomCssJs(JsonNode? payload)
    {
        // Check if tool is enabled - if not, reject the request
        if (!_toolActivation.Enabled.GetValueOrDefault("custom-css-js", false))
        {
            _logger.LogWarning("Attempted to apply Custom CSS/JS while tool is disabled");
            // Return empty state to reflect disabled state
            HandleGetCustomCssJs();
            return;
        }

        var tool = _toolManager?.GetToolInstance("custom-css-js") as Tools.CustomCssJs.CustomCssJsTool;
        if (tool == null)
        {
            // Tool not instantiated yet - enable it first
            _toolManager?.OnToolActivationChanged("custom-css-js", true);
            // Tool is now instantiated synchronously - get it immediately
            tool = _toolManager?.GetToolInstance("custom-css-js") as Tools.CustomCssJs.CustomCssJsTool;
        }

        if (tool != null)
        {
            var css = payload?["css"]?.GetValue<string>();
            var js = payload?["javascript"]?.GetValue<string>();
            tool.UpdateSettings(css, js);
            _logger.LogInformation("Custom CSS/JS updated");
            
            // Send updated state back
            HandleGetCustomCssJs();
        }
        else
        {
            _logger.LogWarning("Custom CSS/JS tool not available");
        }
    }

    private void HandleGetDebugInspectorState()
    {
        var tool = _toolManager?.GetToolInstance("debug-inspector") as Tools.DebugInspector.DebugInspectorTool;
        if (tool != null)
        {
            var state = tool.GetState();
            SendMessage("debug-inspector-state", state);
        }
        else
        {
            // Return empty state if tool not instantiated
            SendMessage("debug-inspector-state", new Tools.DebugInspector.DebugInspectorState
            {
                ConnectionState = _coreState.RuntimeConnectionState.ToString(),
                LastKnownUrl = _coreState.LastKnownUrl,
                ClickUpDesktopStatus = _coreState.ClickUpDesktopStatus.ToString(),
                DebugPortAvailable = _coreState.ClickUpDebugPortAvailable,
                DebugPort = _systemIntegrationSettings.DebugPort,
                RecentNavigations = new List<string>()
            });
        }
    }

    private void PushState()
    {
        // Calculate uptime
        var uptime = DateTime.Now - _coreState.StartTime;
        var uptimeString = uptime.TotalHours >= 1
            ? $"{(int)uptime.TotalHours}h {uptime.Minutes}m"
            : $"{uptime.Minutes}m";

        var state = new
        {
            version = _coreState.Version,
            dotNetVersion = _coreState.DotNetVersion,
            webView2Version = _coreState.WebView2Version,
            logFilePath = _coreState.LogFilePath,
            uptime = uptimeString,
            clickUpDesktopStatus = _coreState.ClickUpDesktopStatus.ToString(),
            clickUpDebugPortAvailable = _coreState.ClickUpDebugPortAvailable,
            hasApiToken = _coreState.HasApiToken,
            tokenValid = _coreState.TokenValid,
            clickUpInstallPath = _coreState.ClickUpInstallPath,
            clickUpInstallPathOverride = _systemIntegrationSettings.ClickUpInstallPathOverride,
            debugPort = _systemIntegrationSettings.DebugPort,
            restartIfRunning = _systemIntegrationSettings.RestartIfRunning,
            autostartEnabled = _coreState.AutostartEnabled,
            runtimeConnectionState = _coreState.RuntimeConnectionState.ToString(),
            lastKnownUrl = _coreState.LastKnownUrl,
            lastKnownTaskId = _coreState.LastKnownTaskId,
            tools = ToolRegistry.Tools.Select(t => new
            {
                id = t.Id,
                name = t.Name,
                description = t.Description,
                enabled = _toolActivation.Enabled.GetValueOrDefault(t.Id, false)
            })
        };

        SendMessage("state-changed", state);
    }

    private void SendMessage(string type, object payload)
    {
        if (_webViewFailed || WebView.CoreWebView2 == null)
        {
            return;
        }

        try
        {
            var message = new { type, payload };
            var json = JsonSerializer.Serialize(message);
            WebView.CoreWebView2.PostWebMessageAsJson(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to WebUI: {Type}", type);
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        // _allowClose is true: let the close proceed (disposes WebView2)
        base.OnClosing(e);
    }

    public void ForceClose()
    {
        _allowClose = true;
        Close();
    }

    // Window Control Handlers
    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
        }
        else
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_allowClose)
        {
            Hide();
        }
        else
        {
            ForceClose();
        }
    }

    // Window Dragging
    private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
        {
            // Detect double-click (within 300ms)
            var now = DateTime.Now;
            var timeSinceLastClick = (now - _lastClickTime).TotalMilliseconds;
            _lastClickTime = now;

            if (timeSinceLastClick < 300 && timeSinceLastClick > 0)
            {
                // Double-click detected - toggle maximize/restore
                if (WindowState == WindowState.Maximized)
                {
                    WindowState = WindowState.Normal;
                }
                else
                {
                    WindowState = WindowState.Maximized;
                }
                return;
            }

            // Single click - start dragging
            DragMove();
        }
    }

    // Button hover handlers for icon color changes
    private void MinimizeButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (MinimizeIcon != null)
        {
            MinimizeIcon.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#737373"));
        }
    }

    private void MinimizeButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (MinimizeIcon != null)
        {
            MinimizeIcon.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a3a3a3"));
        }
    }

    private void MaximizeButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (MaximizeIcon != null)
        {
            MaximizeIcon.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#737373"));
        }
    }

    private void MaximizeButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (MaximizeIcon != null)
        {
            MaximizeIcon.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a3a3a3"));
        }
    }

    private void CloseButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (CloseIcon != null)
        {
            CloseIcon.Stroke = Brushes.White;
        }
    }

    private void CloseButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (CloseIcon != null)
        {
            CloseIcon.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a3a3a3"));
        }
    }

    // Update maximize button icon when window state changes
    private void OnStateChanged(object? sender, EventArgs e)
    {
        // #region agent log
        try { System.IO.File.AppendAllText(@"c:\dev\clickup-desktop-powertools\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "B", location = "ControlWindow.xaml.cs:707", message = "OnStateChanged entry", data = new { windowState = WindowState.ToString(), maximizeIconNull = MaximizeIcon == null }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        
        if (MaximizeIcon != null)
        {
            try
            {
                if (WindowState == WindowState.Maximized)
                {
                    // #region agent log
                    try { System.IO.File.AppendAllText(@"c:\dev\clickup-desktop-powertools\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "B", location = "ControlWindow.xaml.cs:714", message = "Before Geometry.Parse restore", data = new { }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                    // #endregion
                    
                    // Restore icon (Minimize2 from Lucide)
                    MaximizeIcon.Data = Geometry.Parse("M4 14h6m-6 0v6m0-6l6 6m10-8V6a2 2 0 0 0-2-2h-4m6 6v6a2 2 0 0 1-2 2h-4m-6 0H6a2 2 0 0 1-2-2v-4");
                    
                    // #region agent log
                    try { System.IO.File.AppendAllText(@"c:\dev\clickup-desktop-powertools\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "B", location = "ControlWindow.xaml.cs:718", message = "After Geometry.Parse restore", data = new { }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                    // #endregion
                }
                else
                {
                    // #region agent log
                    try { System.IO.File.AppendAllText(@"c:\dev\clickup-desktop-powertools\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "B", location = "ControlWindow.xaml.cs:723", message = "Before Geometry.Parse maximize", data = new { }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                    // #endregion
                    
                    // Maximize icon
                    MaximizeIcon.Data = Geometry.Parse("M8 3H5a2 2 0 0 0-2 2v3m18 0V5a2 2 0 0 0-2-2h-3m0 18h3a2 2 0 0 0 2-2v-3M3 16v3a2 2 0 0 0 2 2h3");
                    
                    // #region agent log
                    try { System.IO.File.AppendAllText(@"c:\dev\clickup-desktop-powertools\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "B", location = "ControlWindow.xaml.cs:727", message = "After Geometry.Parse maximize", data = new { }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                    // #endregion
                }
            }
            catch (Exception ex)
            {
                // #region agent log
                try { System.IO.File.AppendAllText(@"c:\dev\clickup-desktop-powertools\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "B", location = "ControlWindow.xaml.cs:732", message = "Geometry.Parse exception", data = new { exceptionType = ex.GetType().Name, message = ex.Message, stackTrace = ex.StackTrace }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                _logger?.LogError(ex, "Failed to update maximize icon");
            }
        }
        else
        {
            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\dev\clickup-desktop-powertools\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "C", location = "ControlWindow.xaml.cs:738", message = "MaximizeIcon is null", data = new { }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
        }
    }
}

