using System;
using System.Threading.Tasks;

namespace ClickUpDesktopPowerTools.Core;

/// <summary>
/// Minimal, safe API for tools to interact with ClickUp Desktop runtime.
/// Wraps RuntimeBridge to hide CDP details from tools.
/// Per RULE.md: This is a stability boundary, not a framework abstraction.
/// </summary>
public class RuntimeContext
{
    private readonly RuntimeBridge _bridge;

    public RuntimeContext(RuntimeBridge bridge)
    {
        _bridge = bridge;
        _bridge.NavigationOccurred += OnNavigationOccurred;
    }

    /// <summary>
    /// Execute JavaScript in the runtime context with structured result.
    /// Checks for exceptionDetails to determine success, not just return value.
    /// </summary>
    public async Task<ScriptExecutionResult> ExecuteScriptWithResultAsync(string js)
    {
        return await _bridge.ExecuteScriptWithResultAsync(js);
    }

    /// <summary>
    /// Execute JavaScript in the runtime context.
    /// Returns null on error or if result is not a string.
    /// Legacy method for backward compatibility.
    /// </summary>
    public async Task<string?> ExecuteScriptAsync(string js)
    {
        return await _bridge.ExecuteScriptAsync(js);
    }

    /// <summary>
    /// Get task ID from URL using injected helper.
    /// Returns null if helper fails or task ID cannot be extracted.
    /// </summary>
    public async Task<string?> GetTaskIdAsync()
    {
        try
        {
            var result = await ExecuteScriptAsync("(window.getTaskIdFromUrl && window.getTaskIdFromUrl()) || null");
            return string.IsNullOrEmpty(result) || result == "null" ? null : result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Navigation event - best-effort, may miss events.
    /// </summary>
    public event EventHandler<string>? NavigationOccurred;

    private void OnNavigationOccurred(object? sender, string url)
    {
        NavigationOccurred?.Invoke(this, url);
    }
}

