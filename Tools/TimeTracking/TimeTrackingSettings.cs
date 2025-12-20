using ClickUpDesktopPowerTools.Core;

namespace ClickUpDesktopPowerTools.Tools.TimeTracking;

public sealed class TimeTrackingSettings
{
    public string? TeamId { get; set; }

    public static TimeTrackingSettings Load()
    {
        return SettingsManager.Load<TimeTrackingSettings>("TimeTracking");
    }

    public void Save()
    {
        SettingsManager.Save("TimeTracking", this);
    }
}

