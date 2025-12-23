using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ClickUpDesktopPowerTools.Core;

public class AppStartup
{
    private readonly ILogger<AppStartup> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private TrayHost? _trayHost;
    private TokenStorage? _tokenStorage;
    private ClickUpApi? _clickUpApi;
    private ClickUpRuntime? _clickUpRuntime;
    private RuntimeBridge? _runtimeBridge;
    private ToolManager? _toolManager;
    private CoreState? _coreState;
    private SystemIntegration? _systemIntegration;
    private SystemIntegrationSettings? _systemIntegrationSettings;

    public AppStartup(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<AppStartup>();
    }

    public void Initialize()
    {
        _logger.LogInformation("Initializing ClickUp Desktop PowerTools");

        // Create token storage and API
        _tokenStorage = new TokenStorage();
        var apiLogger = _loggerFactory.CreateLogger<ClickUpApi>();
        _clickUpApi = new ClickUpApi(_tokenStorage, apiLogger);

        // Create runtime detection
        _clickUpRuntime = new ClickUpRuntime(_loggerFactory.CreateLogger<ClickUpRuntime>());

        // Load system integration settings and create integration
        _systemIntegrationSettings = SystemIntegrationSettings.Load();
        _systemIntegration = new SystemIntegration(
            _loggerFactory.CreateLogger<SystemIntegration>());

        // Compute log file path (matches SimpleFileLoggerProvider)
        var logFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClickUpDesktopPowerTools", "logs", "app.log");

        // Create core state with runtime info
        _coreState = new CoreState
        {
            HasApiToken = !string.IsNullOrEmpty(_tokenStorage.GetToken()),
            LogFilePath = logFilePath,
            ClickUpDesktopStatus = _clickUpRuntime.CheckStatus(),
            ClickUpInstallPath = _systemIntegration.ResolveClickUpInstallPath(_systemIntegrationSettings),
            AutostartEnabled = _systemIntegration.ReadAutostartEnabled()
        };

        // Load tool activation settings and update CoreState
        var toolActivation = SettingsManager.Load<ToolActivationSettings>("ToolActivation");
        _coreState.ActiveTools = new Dictionary<string, bool>(toolActivation.Enabled);

        _logger.LogInformation("Core version: {Version}, .NET: {DotNet}, ClickUp Desktop: {Status}",
            _coreState.Version, _coreState.DotNetVersion, _coreState.ClickUpDesktopStatus);

        // Create runtime bridge and tool manager
        _runtimeBridge = new RuntimeBridge(
            _loggerFactory.CreateLogger<RuntimeBridge>(),
            _clickUpRuntime,
            _systemIntegrationSettings);
        
        _toolManager = new ToolManager(_loggerFactory.CreateLogger<ToolManager>());

        // Wire runtime bridge events
        _runtimeBridge.ConnectionStateChanged += (sender, state) =>
        {
            _coreState.RuntimeConnectionState = state;
            
            // Notify ToolManager of connection state changes
            if (state == RuntimeConnectionState.Connected)
            {
                var ctx = new RuntimeContext(_runtimeBridge);
                _toolManager.OnRuntimeConnected(ctx);
            }
            else if (state == RuntimeConnectionState.Disconnected || state == RuntimeConnectionState.Failed)
            {
                _toolManager.OnRuntimeDisconnected();
            }
        };
        
        _runtimeBridge.NavigationOccurred += async (sender, url) =>
        {
            _coreState.LastKnownUrl = url;
            // Try to extract task ID
            if (_runtimeBridge.ConnectionState == RuntimeConnectionState.Connected)
            {
                var taskId = await _runtimeBridge.ExecuteScriptAsync("(window.getTaskIdFromUrl && window.getTaskIdFromUrl()) || null");
                if (!string.IsNullOrEmpty(taskId) && taskId != "null")
                {
                    _coreState.LastKnownTaskId = taskId;
                }
            }
        };

        // Register tools
        RegisterTools();

        // Initialize tray host with dependencies (including runtime bridge and tool manager)
        _trayHost = new TrayHost(_tokenStorage, _clickUpApi, _coreState, 
            _clickUpRuntime, _systemIntegration, _systemIntegrationSettings, 
            _runtimeBridge, _toolManager, _loggerFactory);
        _trayHost.Initialize();

        _logger.LogInformation("Tray host initialized");

        // Try to connect runtime bridge if ClickUp is running
        // Connection state change will notify ToolManager automatically
        if (_clickUpRuntime.CheckStatus() == ClickUpDesktopStatus.Running)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(2)); // Give ClickUp time to start CDP
                await _runtimeBridge.ConnectAsync(_systemIntegrationSettings.DebugPort);
            });
        }
    }

    private void RegisterTools()
    {
        if (_toolManager == null) return;

        // Register time-tracking tool
        _toolManager.RegisterTool("time-tracking", () =>
        {
            var serviceLogger = _loggerFactory.CreateLogger<Tools.TimeTracking.TimeTrackingService>();
            var service = new Tools.TimeTracking.TimeTrackingService(_clickUpApi!, serviceLogger);
            var viewModel = new Tools.TimeTracking.TimeTrackingViewModel(service);
            return viewModel;
        });

        // Register custom CSS/JS tool
        _toolManager.RegisterTool("custom-css-js", () =>
        {
            var toolLogger = _loggerFactory.CreateLogger<Tools.CustomCssJs.CustomCssJsTool>();
            return new Tools.CustomCssJs.CustomCssJsTool(toolLogger);
        });

        // Register debug inspector tool
        _toolManager.RegisterTool("debug-inspector", () =>
        {
            var toolLogger = _loggerFactory.CreateLogger<Tools.DebugInspector.DebugInspectorTool>();
            return new Tools.DebugInspector.DebugInspectorTool(
                toolLogger,
                _runtimeBridge!,
                _clickUpRuntime!,
                _systemIntegrationSettings!);
        });

        // Load tool activation and notify tool manager
        var toolActivation = SettingsManager.Load<ToolActivationSettings>("ToolActivation");
        foreach (var tool in ToolRegistry.Tools)
        {
            var enabled = toolActivation.Enabled.GetValueOrDefault(tool.Id, false);
            if (enabled)
            {
                _toolManager.OnToolActivationChanged(tool.Id, true);
            }
        }

        _logger.LogInformation("Tools registered");
    }

    public void Shutdown()
    {
        _runtimeBridge?.Dispose();
        _trayHost?.Dispose();
        _logger.LogInformation("Application shutting down");
    }
}
