using System;
using System.Collections.Generic;
using System.Linq;
using ClickUpDesktopPowerTools.Core;
using Microsoft.Extensions.Logging;

namespace ClickUpDesktopPowerTools.Tools.DebugInspector;

/// <summary>
/// Read-only debug and inspection tool for RuntimeBridge validation.
/// Observes runtime state without mutating behavior.
/// </summary>
public class DebugInspectorTool : IToolLifecycle
{
    private readonly ILogger<DebugInspectorTool> _logger;
    private readonly RuntimeBridge _runtimeBridge;
    private readonly ClickUpRuntime _clickUpRuntime;
    private readonly SystemIntegrationSettings _systemIntegrationSettings;
    private RuntimeContext? _runtimeContext;
    private readonly List<string> _recentNavigations = new();
    private const int MaxNavigationHistory = 10;

    public DebugInspectorTool(
        ILogger<DebugInspectorTool> logger,
        RuntimeBridge runtimeBridge,
        ClickUpRuntime clickUpRuntime,
        SystemIntegrationSettings systemIntegrationSettings)
    {
        _logger = logger;
        _runtimeBridge = runtimeBridge;
        _clickUpRuntime = clickUpRuntime;
        _systemIntegrationSettings = systemIntegrationSettings;

        // Subscribe to bridge events
        _runtimeBridge.ConnectionStateChanged += OnConnectionStateChanged;
        _runtimeBridge.NavigationOccurred += OnNavigationOccurred;
    }

    public RuntimeConnectionState ConnectionState => _runtimeBridge.ConnectionState;
    public string? LastKnownUrl => _runtimeBridge.LastKnownUrl;
    public ClickUpDesktopStatus ClickUpDesktopStatus => _clickUpRuntime.CheckStatus();
    public bool? DebugPortAvailable => _clickUpRuntime.DebugPortAvailable;
    public int DebugPort => _systemIntegrationSettings.DebugPort;
    public IReadOnlyList<string> RecentNavigations => _recentNavigations.AsReadOnly();

    public void OnEnable()
    {
        _logger.LogInformation("Debug Inspector tool enabled");
    }

    public void OnDisable()
    {
        _runtimeContext = null;
        _logger.LogInformation("Debug Inspector tool disabled");
    }

    public void OnRuntimeReady(RuntimeContext ctx)
    {
        _runtimeContext = ctx;
        _logger.LogInformation("Runtime ready for Debug Inspector");
        
        // Subscribe to navigation events
        ctx.NavigationOccurred += OnRuntimeNavigationOccurred;
    }

    public void OnRuntimeDisconnected()
    {
        if (_runtimeContext != null)
        {
            _runtimeContext.NavigationOccurred -= OnRuntimeNavigationOccurred;
        }
        _runtimeContext = null;
        _logger.LogInformation("Runtime disconnected for Debug Inspector");
    }

    private void OnConnectionStateChanged(object? sender, RuntimeConnectionState state)
    {
        _logger.LogDebug("Connection state changed: {State}", state);
    }

    private void OnNavigationOccurred(object? sender, string url)
    {
        AddNavigation(url);
    }

    private void OnRuntimeNavigationOccurred(object? sender, string url)
    {
        AddNavigation(url);
    }

    private void AddNavigation(string url)
    {
        lock (_recentNavigations)
        {
            _recentNavigations.Insert(0, $"{DateTime.Now:HH:mm:ss} - {url}");
            if (_recentNavigations.Count > MaxNavigationHistory)
            {
                _recentNavigations.RemoveAt(_recentNavigations.Count - 1);
            }
        }
    }

    public DebugInspectorState GetState()
    {
        return new DebugInspectorState
        {
            ConnectionState = ConnectionState.ToString(),
            LastKnownUrl = LastKnownUrl,
            ClickUpDesktopStatus = ClickUpDesktopStatus.ToString(),
            DebugPortAvailable = DebugPortAvailable,
            DebugPort = DebugPort,
            RecentNavigations = RecentNavigations.ToList()
        };
    }
}

public class DebugInspectorState
{
    public string ConnectionState { get; set; } = string.Empty;
    public string? LastKnownUrl { get; set; }
    public string ClickUpDesktopStatus { get; set; } = string.Empty;
    public bool? DebugPortAvailable { get; set; }
    public int DebugPort { get; set; }
    public List<string> RecentNavigations { get; set; } = new();
}

