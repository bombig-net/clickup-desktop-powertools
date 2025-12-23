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

    // API status (mutable)
    public bool HasApiToken { get; set; }
    // TokenValid is nullable: null = not tested, true = valid, false = invalid
    public bool? TokenValid { get; set; }

    // Runtime bridge status (mutable)
    public ClickUpDesktopStatus ClickUpDesktopStatus { get; set; } = ClickUpDesktopStatus.Unknown;
    // Debug port availability: null = not checked, true = available, false = not available
    public bool? ClickUpDebugPortAvailable { get; set; }

    // System integration state (derived at startup, refreshable)
    public string? ClickUpInstallPath { get; set; }  // Resolved path (override or auto-detected)
    public bool AutostartEnabled { get; set; }
}

