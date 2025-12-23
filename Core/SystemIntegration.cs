using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace ClickUpDesktopPowerTools.Core;

public class SystemIntegration
{
    private readonly ILogger<SystemIntegration> _logger;
    private const string AutostartKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AutostartValueName = "ClickUpDesktopPowerTools";

    public SystemIntegration(ILogger<SystemIntegration> logger)
    {
        _logger = logger;
    }

    // --- ClickUp Path Detection ---

    public string? DetectClickUpInstallPath()
    {
        // Strategy 1: Check common install locations (fast, no process access)
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData), "ClickUp", "ClickUp.exe"),
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ProgramFiles), "ClickUp", "ClickUp.exe"),
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ProgramFilesX86), "ClickUp", "ClickUp.exe")
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                _logger.LogDebug("ClickUp found at: {Path}", path);
                return path;
            }
        }

        // Strategy 2: If running, try to get path from process
        return TryGetPathFromRunningProcess();
    }

    private string? TryGetPathFromRunningProcess()
    {
        Process[] processes = Array.Empty<Process>();
        try
        {
            processes = Process.GetProcessesByName("ClickUp");
            if (processes.Length == 0) return null;

            // MainModule can throw Win32Exception (access denied) or
            // InvalidOperationException (process exited)
            var path = processes[0].MainModule?.FileName;
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                _logger.LogDebug("ClickUp path from process: {Path}", path);
                return path;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not get ClickUp path from process");
        }
        finally
        {
            foreach (var p in processes) p.Dispose();
        }
        return null;
    }

    public string? ResolveClickUpInstallPath(SystemIntegrationSettings settings)
    {
        // Priority 1: User override (explicit configuration)
        if (!string.IsNullOrEmpty(settings.ClickUpInstallPathOverride) 
            && File.Exists(settings.ClickUpInstallPathOverride))
        {
            _logger.LogDebug("Using configured ClickUp path: {Path}", settings.ClickUpInstallPathOverride);
            return settings.ClickUpInstallPathOverride;
        }
        
        // Priority 2: Auto-detection (fallback)
        return DetectClickUpInstallPath();
    }

    // --- Launch ClickUp Debug Mode ---

    public (bool Success, string? Error) LaunchClickUpDebugMode(
        string? installPath, 
        SystemIntegrationSettings settings)
    {
        // Check if ClickUp is running
        var processes = Process.GetProcessesByName("ClickUp");
        var isRunning = processes.Length > 0;
        
        if (isRunning)
        {
            if (settings.RestartIfRunning)
            {
                // Kill existing processes
                foreach (var p in processes)
                {
                    try
                    {
                        p.Kill();
                        p.WaitForExit(5000);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to kill ClickUp process {Id}", p.Id);
                    }
                    finally
                    {
                        p.Dispose();
                    }
                }
                
                // Brief delay to ensure cleanup
                Thread.Sleep(500);
            }
            else
            {
                foreach (var p in processes) p.Dispose();
                return (false, "ClickUp is already running. Enable 'Restart if running' to close it first.");
            }
        }
        else
        {
            foreach (var p in processes) p.Dispose();
        }

        if (string.IsNullOrEmpty(installPath) || !File.Exists(installPath))
        {
            return (false, "ClickUp installation not found.");
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = installPath,
                Arguments = $"--remote-debugging-port={settings.DebugPort}",
                UseShellExecute = true
            });
            _logger.LogInformation("Launched ClickUp in debug mode on port {Port}", settings.DebugPort);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch ClickUp");
            return (false, $"Failed to launch: {ex.Message}");
        }
    }

    // --- Autostart ---

    public bool ReadAutostartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(AutostartKey, writable: false);
            var value = key?.GetValue(AutostartValueName) as string;
            if (string.IsNullOrEmpty(value)) return false;

            // Verify it points to current executable (path may have changed)
            var currentPath = Environment.ProcessPath;
            var registeredPath = value.Trim('"');
            return string.Equals(registeredPath, currentPath, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read autostart state");
            return false;
        }
    }

    public bool SetAutostartEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(AutostartKey, writable: true);
            if (key == null)
            {
                _logger.LogWarning("Could not open Run key for writing");
                return false;
            }

            if (enabled)
            {
                var exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath)) return false;

                // Quote path to handle spaces
                var value = exePath.Contains(' ') ? $"\"{exePath}\"" : exePath;
                key.SetValue(AutostartValueName, value, RegistryValueKind.String);
                _logger.LogInformation("Autostart enabled");
            }
            else
            {
                key.DeleteValue(AutostartValueName, throwOnMissingValue: false);
                _logger.LogInformation("Autostart disabled");
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set autostart");
            return false;
        }
    }

    // --- Folder Opening ---

    public void OpenFolder(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;

        var folder = File.Exists(path) ? Path.GetDirectoryName(path) : path;
        if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
        {
            Process.Start("explorer.exe", folder);
        }
    }
}

