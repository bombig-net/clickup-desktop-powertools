using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using ClickUpDesktopPowerTools.Core;

namespace ClickUpDesktopPowerTools.UI.Control;

public partial class ControlWindow : Window
{
    private readonly TokenStorage _tokenStorage;
    private readonly ClickUpApi _clickUpApi;
    private readonly CoreState _coreState;
    private readonly ClickUpRuntime _clickUpRuntime;
    private readonly SystemIntegration _systemIntegration;
    private readonly SystemIntegrationSettings _systemIntegrationSettings;
    private readonly ILogger<ControlWindow> _logger;
    private readonly ToolActivationSettings _toolActivation;

    private bool _allowClose = false;
    private bool _isInitialized = false;
    private bool _webViewFailed = false;
    private bool _hasAttemptedReload = false;

    public ControlWindow(TokenStorage tokenStorage, ClickUpApi clickUpApi, CoreState coreState, ClickUpRuntime clickUpRuntime, SystemIntegration systemIntegration, SystemIntegrationSettings systemIntegrationSettings, ILoggerFactory loggerFactory)
    {
        _tokenStorage = tokenStorage;
        _clickUpApi = clickUpApi;
        _coreState = coreState;
        _clickUpRuntime = clickUpRuntime;
        _systemIntegration = systemIntegration;
        _systemIntegrationSettings = systemIntegrationSettings;
        _logger = loggerFactory.CreateLogger<ControlWindow>();
        _toolActivation = SettingsManager.Load<ToolActivationSettings>("ToolActivation");

        InitializeComponent();
        Loaded += OnLoaded;
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
        if (e.IsSuccess && !_webViewFailed)
        {
            // Push initial state to WebUI
            PushState();
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

                case "open-log-folder":
                    HandleOpenLogFolder();
                    break;

                case "refresh-runtime-status":
                    HandleRefreshRuntimeStatus();
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
}

