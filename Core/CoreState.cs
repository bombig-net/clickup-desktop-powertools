using System;

namespace ClickUpDesktopPowerTools.Core;

public class CoreState
{
    // App identity
    public string Version { get; init; } = "1.0.0";

    // Runtime info (set once at startup)
    public string DotNetVersion { get; init; } = Environment.Version.ToString();
    public string? WebView2Version { get; set; } // Set after WebView2 init
    public string LogFilePath { get; init; } = string.Empty;
    public DateTime StartTime { get; init; } = DateTime.Now;

    // Runtime bridge status (mutable)
    public ClickUpDesktopStatus ClickUpDesktopStatus { get; set; } = ClickUpDesktopStatus.Unknown;
    // Debug port availability: null = not checked, true = available, false = not available
    public bool? ClickUpDebugPortAvailable { get; set; }

    // Runtime connection state (mutable, thread-safe)
    private readonly object _runtimeStateLock = new object();
    private RuntimeConnectionState _runtimeConnectionState = RuntimeConnectionState.Disconnected;
    private string? _lastKnownUrl;
    private string? _lastKnownTaskId;

    public RuntimeConnectionState RuntimeConnectionState
    {
        get { lock (_runtimeStateLock) return _runtimeConnectionState; }
        set { lock (_runtimeStateLock) _runtimeConnectionState = value; }
    }

    public string? LastKnownUrl
    {
        get { lock (_runtimeStateLock) return _lastKnownUrl; }
        set { lock (_runtimeStateLock) _lastKnownUrl = value; }
    }

    public string? LastKnownTaskId
    {
        get { lock (_runtimeStateLock) return _lastKnownTaskId; }
        set { lock (_runtimeStateLock) _lastKnownTaskId = value; }
    }

    public Dictionary<string, bool> ActiveTools { get; set; } = new();  // toolId -> enabled

    // System integration state (derived at startup, refreshable)
    public string? ClickUpInstallPath { get; set; }  // Resolved path (override or auto-detected)
    public bool AutostartEnabled { get; set; }
}

public enum RuntimeConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Failed
}

