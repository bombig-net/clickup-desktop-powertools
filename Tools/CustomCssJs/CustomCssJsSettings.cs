using ClickUpDesktopPowerTools.Core;

namespace ClickUpDesktopPowerTools.Tools.CustomCssJs;

public sealed class CustomCssJsSettings
{
    public string? CustomCss { get; set; }
    public string? CustomJavaScript { get; set; }

    public static CustomCssJsSettings Load()
    {
        return SettingsManager.Load<CustomCssJsSettings>("CustomCssJs");
    }

    public void Save()
    {
        SettingsManager.Save("CustomCssJs", this);
    }
}

