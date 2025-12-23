using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly HttpClient _httpClient;

    public ClickUpDesktopStatus Status { get; private set; } = ClickUpDesktopStatus.Unknown;
    public bool? DebugPortAvailable { get; private set; }

    public ClickUpRuntime(ILogger<ClickUpRuntime> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };
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

    public async Task<bool?> CheckDebugPortAvailability(int port)
    {
        try
        {
            var url = $"http://localhost:{port}/json/version";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                DebugPortAvailable = true;
                _logger.LogDebug("Debug port {Port} is available", port);
                return true;
            }
            else
            {
                DebugPortAvailable = false;
                _logger.LogDebug("Debug port {Port} returned status {Status}", port, response.StatusCode);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            DebugPortAvailable = false;
            _logger.LogDebug(ex, "Debug port {Port} is not reachable", port);
            return false;
        }
        catch (TaskCanceledException)
        {
            DebugPortAvailable = false;
            _logger.LogDebug("Debug port {Port} check timed out", port);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check debug port {Port} availability", port);
            DebugPortAvailable = null;
            return null;
        }
    }
}

