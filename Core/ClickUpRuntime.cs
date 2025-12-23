using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ClickUpDesktopPowerTools.Core;

public enum ClickUpDesktopStatus { NotRunning, Running, Unknown }

/// <summary>
/// Core-level runtime detection for ClickUp Desktop.
/// Per RULE.md: Core owns "ClickUp Desktop runtime communication" at platform level.
/// This v1 only detects process presence; actual CDP connection is deferred.
/// </summary>
public class ClickUpRuntime
{
    private readonly ILogger<ClickUpRuntime> _logger;

    public ClickUpDesktopStatus Status { get; private set; } = ClickUpDesktopStatus.Unknown;

    public ClickUpRuntime(ILogger<ClickUpRuntime> logger)
    {
        _logger = logger;
    }

    public ClickUpDesktopStatus CheckStatus()
    {
        try
        {
            // ClickUp Desktop process name (Electron app)
            var processes = Process.GetProcessesByName("ClickUp");
            Status = processes.Length > 0
                ? ClickUpDesktopStatus.Running
                : ClickUpDesktopStatus.NotRunning;

            // Dispose process handles
            foreach (var p in processes)
            {
                p.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check ClickUp Desktop status");
            Status = ClickUpDesktopStatus.Unknown;
        }

        return Status;
    }
}

