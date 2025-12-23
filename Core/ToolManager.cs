using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace ClickUpDesktopPowerTools.Core;

/// <summary>
/// Manages tool lifecycle and wires enabled tools to runtime availability.
/// Per RULE.md: Core manages platform-level lifecycle, not tool-specific logic.
/// </summary>
public class ToolManager
{
    private readonly ILogger<ToolManager> _logger;
    private readonly Dictionary<string, Func<IToolLifecycle>> _toolFactories = new();
    private readonly Dictionary<string, IToolLifecycle> _toolInstances = new();
    private RuntimeContext? _runtimeContext;
    private readonly object _lock = new object();

    public ToolManager(ILogger<ToolManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Register a tool factory. Tools are instantiated lazily when enabled.
    /// </summary>
    public void RegisterTool(string toolId, Func<IToolLifecycle> factory)
    {
        lock (_lock)
        {
            _toolFactories[toolId] = factory;
        }
    }

    /// <summary>
    /// Handle tool activation change.
    /// </summary>
    public void OnToolActivationChanged(string toolId, bool enabled)
    {
        lock (_lock)
        {
            if (enabled)
            {
                // Ensure tool instance exists
                if (!_toolInstances.TryGetValue(toolId, out var tool))
                {
                    if (_toolFactories.TryGetValue(toolId, out var factory))
                    {
                        tool = factory();
                        _toolInstances[toolId] = tool;
                        _logger.LogInformation("Instantiated tool: {ToolId}", toolId);
                    }
                    else
                    {
                        _logger.LogWarning("Tool factory not found: {ToolId}", toolId);
                        return;
                    }
                }

                // Call OnEnable
                CallToolLifecycle(toolId, tool, t => t.OnEnable());

                // If runtime is ready, notify tool immediately
                if (_runtimeContext != null)
                {
                    CallToolLifecycle(toolId, tool, t => t.OnRuntimeReady(_runtimeContext));
                }
            }
            else
            {
                if (_toolInstances.TryGetValue(toolId, out var tool))
                {
                    // If runtime is connected, notify disconnection first
                    if (_runtimeContext != null)
                    {
                        CallToolLifecycle(toolId, tool, t => t.OnRuntimeDisconnected());
                    }

                    // Then disable
                    CallToolLifecycle(toolId, tool, t => t.OnDisable());
                }
            }
        }
    }

    /// <summary>
    /// Handle runtime connection.
    /// </summary>
    public void OnRuntimeConnected(RuntimeContext ctx)
    {
        lock (_lock)
        {
            _runtimeContext = ctx;

            // Notify all enabled tools
            var enabledTools = GetEnabledTools();
            foreach (var (toolId, tool) in enabledTools)
            {
                CallToolLifecycle(toolId, tool, t => t.OnRuntimeReady(ctx));
            }
        }
    }

    /// <summary>
    /// Handle runtime disconnection.
    /// </summary>
    public void OnRuntimeDisconnected()
    {
        lock (_lock)
        {
            _runtimeContext = null;

            // Notify all enabled tools
            var enabledTools = GetEnabledTools();
            foreach (var (toolId, tool) in enabledTools)
            {
                CallToolLifecycle(toolId, tool, t => t.OnRuntimeDisconnected());
            }
        }
    }

    private List<(string toolId, IToolLifecycle tool)> GetEnabledTools()
    {
        var enabled = new List<(string, IToolLifecycle)>();
        var settings = SettingsManager.Load<ToolActivationSettings>("ToolActivation");
        
        foreach (var (toolId, tool) in _toolInstances)
        {
            if (settings.Enabled.GetValueOrDefault(toolId, false))
            {
                enabled.Add((toolId, tool));
            }
        }
        
        return enabled;
    }

    /// <summary>
    /// Get a tool instance by ID. Returns null if tool is not instantiated or not found.
    /// </summary>
    public IToolLifecycle? GetToolInstance(string toolId)
    {
        lock (_lock)
        {
            _toolInstances.TryGetValue(toolId, out var tool);
            return tool;
        }
    }

    /// <summary>
    /// Exception isolation wrapper - prevents tool failures from crashing Core.
    /// </summary>
    private void CallToolLifecycle(string toolId, IToolLifecycle tool, Action<IToolLifecycle> action)
    {
        try
        {
            action(tool);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool {ToolId} lifecycle method failed", toolId);
            // Don't rethrow - isolate tool failures
        }
    }
}

