using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        var path = GetSettingsFilePath();

        if (!File.Exists(path))
        {
            var defaults = new OverlayPlacementSettings();
            TrySaveToPath(defaults, path);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<OverlayPlacementSettings>(json, GetJsonOptions());
            if (settings == null)
            {
                return new OverlayPlacementSettings();
            }

            if (!Enum.IsDefined(typeof(OverlayDock), settings.OverlayDock))
            {
                settings.OverlayDock = OverlayDock.Right;
            }

            return settings;
        }
        catch
        {
            return new OverlayPlacementSettings();
        }
    }

    public void Save()
    {
        TrySaveToPath(this, GetSettingsFilePath());
    }

    private static void TrySaveToPath(OverlayPlacementSettings settings, string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(settings, GetJsonOptions());
            File.WriteAllText(path, json);
        }
        catch
        {
            // Persistence should never prevent app startup.
        }
    }

    private static string GetSettingsFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "ClickUpDesktopPowerTools", "settings.json");
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }
}

