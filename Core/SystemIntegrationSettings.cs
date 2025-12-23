namespace ClickUpDesktopPowerTools.Core;

public sealed class SystemIntegrationSettings
{
    // Override auto-detected ClickUp path (null = use auto-detection)
    public string? ClickUpInstallPathOverride { get; set; }
    
    // Debug port for remote debugging (default 9222)
    public int DebugPort { get; set; } = 9222;
    
    // If true, kill existing ClickUp process before launching in debug mode
    public bool RestartIfRunning { get; set; } = false;

    public static SystemIntegrationSettings Load()
    {
        var settings = SettingsManager.Load<SystemIntegrationSettings>("SystemIntegration");
        
        // Validate debug port range
        if (settings.DebugPort < 1024 || settings.DebugPort > 65535)
        {
            settings.DebugPort = 9222;
        }
        
        return settings;
    }

    public void Save()
    {
        SettingsManager.Save("SystemIntegration", this);
    }
}

