using System.Collections.Generic;

namespace ClickUpDesktopPowerTools.Core;

/// <summary>
/// Hardcoded tool metadata for UI display only.
/// Per RULE.md: "No dynamic plugin loading"
/// </summary>
public static class ToolRegistry
{
    public static readonly ToolInfo[] Tools = new[]
    {
        new ToolInfo("custom-css-js", "Custom CSS/JS",
            "Apply custom styling and scripts to ClickUp Desktop")
    };

    public record ToolInfo(string Id, string Name, string Description);
}

/// <summary>
/// Persisted via existing SettingsManager.
/// Core stores activation state; tools query it themselves.
/// </summary>
public class ToolActivationSettings
{
    public Dictionary<string, bool> Enabled { get; set; } = new();
}

