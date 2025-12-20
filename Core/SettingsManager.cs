using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ClickUpDesktopPowerTools.Core;

public static class SettingsManager
{
    private static readonly object _lock = new object();
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static string GetSettingsFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "ClickUpDesktopPowerTools", "settings.json");
    }

    public static T Load<T>(string section) where T : new()
    {
        lock (_lock)
        {
            var path = GetSettingsFilePath();

            if (!File.Exists(path))
            {
                return new T();
            }

            try
            {
                var json = File.ReadAllText(path);
                var root = JsonNode.Parse(json);

                if (root == null)
                {
                    return new T();
                }

                // Check if this is old flat format (root-level properties instead of sections)
                if (root is JsonObject rootObj && !rootObj.ContainsKey(section))
                {
                    // Try to detect if this is old format by checking for known property names
                    // If section doesn't exist, try to migrate from old format
                    if (TryMigrateFromOldFormat<T>(rootObj, section, out var migrated))
                    {
                        return migrated;
                    }
                }

                // Load from namespaced section
                if (root is JsonObject obj && obj.TryGetPropertyValue(section, out var sectionNode))
                {
                    var settings = sectionNode.Deserialize<T>(_jsonOptions);
                    return settings ?? new T();
                }

                return new T();
            }
            catch
            {
                // Persistence should never prevent app startup.
                return new T();
            }
        }
    }

    public static void Save<T>(string section, T settings)
    {
        lock (_lock)
        {
            try
            {
                var path = GetSettingsFilePath();
                JsonObject root;

                // Load existing file or create new
                if (File.Exists(path))
                {
                    try
                    {
                        var json = File.ReadAllText(path);
                        var parsed = JsonNode.Parse(json);
                        root = parsed as JsonObject ?? new JsonObject();
                    }
                    catch
                    {
                        root = new JsonObject();
                    }
                }
                else
                {
                    root = new JsonObject();
                }

                // Migrate old flat format if detected
                MigrateOldFormatIfNeeded(root);

                // Serialize the settings to a JsonNode
                var sectionJson = JsonSerializer.SerializeToNode(settings, _jsonOptions);
                if (sectionJson != null)
                {
                    // Replace the section (merge semantics: replace section, preserve all others)
                    root[section] = sectionJson;
                }

                // Write back atomically
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var options = new JsonSerializerOptions(_jsonOptions);
                var jsonText = JsonSerializer.Serialize(root, options);
                File.WriteAllText(path, jsonText);
            }
            catch
            {
                // Persistence should never prevent app startup.
            }
        }
    }

    private static bool TryMigrateFromOldFormat<T>(JsonObject root, string section, out T settings) where T : new()
    {
        settings = default(T)!;
        
        // Try to deserialize from root level (old format)
        try
        {
            var deserialized = root.Deserialize<T>(_jsonOptions);
            if (deserialized != null)
            {
                // Found old format data, return it (migration to namespaced will happen on next Save)
                settings = deserialized;
                return true;
            }
        }
        catch
        {
            // Not old format or deserialization failed
        }

        return false;
    }

    private static void MigrateOldFormatIfNeeded(JsonObject root)
    {
        // Check if this looks like old flat format (has known property names at root level)
        // Known properties from OverlayPlacementSettings
        if (root.ContainsKey("OverlayDock") || root.ContainsKey("OverlayOffset"))
        {
            // Migrate OverlayPlacement settings
            var overlaySection = new JsonObject();
            if (root.TryGetPropertyValue("OverlayDock", out var dockNode))
            {
                overlaySection["OverlayDock"] = dockNode;
                root.Remove("OverlayDock");
            }
            if (root.TryGetPropertyValue("OverlayOffset", out var offsetNode))
            {
                overlaySection["OverlayOffset"] = offsetNode;
                root.Remove("OverlayOffset");
            }
            if (overlaySection.Count > 0)
            {
                root["OverlayPlacement"] = overlaySection;
            }
        }

        // Known properties from TimeTrackingSettings
        if (root.ContainsKey("TeamId"))
        {
            // Migrate TimeTracking settings
            var timeTrackingSection = new JsonObject();
            if (root.TryGetPropertyValue("TeamId", out var teamIdNode))
            {
                timeTrackingSection["TeamId"] = teamIdNode;
                root.Remove("TeamId");
            }
            if (timeTrackingSection.Count > 0)
            {
                root["TimeTracking"] = timeTrackingSection;
            }
        }
    }
}

