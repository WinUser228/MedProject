using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MedProject.Settings;

public class AppSettings
{
    private static readonly string _settingsPath = MedProject.Utilities.AppPaths.SettingsPath;
    private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    private const string SectionName = "AppSettings";

    public int LogRetentionDays
    {
        get => GetInt(SectionName, "LogRetentionDays", defaultValue: 30);
    }

    private static JsonObject LoadRoot()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return new JsonObject();

            var json = File.ReadAllText(_settingsPath);
            return JsonNode.Parse(json) as JsonObject ?? new JsonObject();
        }
        catch
        {
            return new JsonObject();
        }
    }

    private static void SaveRoot(JsonObject root)
    {
        File.WriteAllText(_settingsPath, root.ToJsonString(_options));
    }

    private static int GetInt(string section, string key, int defaultValue)
    {
        try
        {
            var root = LoadRoot();
            return root[section]?[key]?.GetValue<int>() ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }
}
