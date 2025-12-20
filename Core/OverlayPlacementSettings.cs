using System;

namespace ClickUpDesktopPowerTools.Core;

public enum OverlayDock
{
    Left,
    Center,
    Right
}

public sealed class OverlayPlacementSettings
{
    public OverlayDock OverlayDock { get; set; } = OverlayDock.Right;
    public int OverlayOffset { get; set; } = 0;

    public static OverlayPlacementSettings Load()
    {
        var settings = SettingsManager.Load<OverlayPlacementSettings>("OverlayPlacement");

        // Validate OverlayDock enum value
        if (!Enum.IsDefined(typeof(OverlayDock), settings.OverlayDock))
        {
            settings.OverlayDock = OverlayDock.Right;
        }

        return settings;
    }

    public void Save()
    {
        SettingsManager.Save("OverlayPlacement", this);
    }
}

