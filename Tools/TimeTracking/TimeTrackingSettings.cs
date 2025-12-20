using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClickUpDesktopPowerTools.Tools.TimeTracking;

public sealed class TimeTrackingSettings
{
    public string? TeamId { get; set; }

    public static TimeTrackingSettings Load()
    {
        var path = GetSettingsFilePath();

        if (!File.Exists(path))
        {
            var defaults = new TimeTrackingSettings();
            TrySaveToPath(defaults, path);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<TimeTrackingSettings>(json, GetJsonOptions());
            if (settings == null)
            {
                return new TimeTrackingSettings();
            }

            return settings;
        }
        catch
        {
            return new TimeTrackingSettings();
        }
    }

    public void Save()
    {
        TrySaveToPath(this, GetSettingsFilePath());
    }

    private static void TrySaveToPath(TimeTrackingSettings settings, string path)
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

