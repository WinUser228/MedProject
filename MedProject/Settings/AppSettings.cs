using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MedProject.Settings;

public class AppSettings
{
    private static readonly string _settingsPath = MedProject.Utilities.AppPaths.SettingsPath;

    private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public bool IsFirstRun
    {
        get => GetBool("AppSettings:IsFirstRun", true);
        set => SetValue("AppSettings", "IsFirstRun", value);
    }

    public int LogRetentionDays
    {
        get => GetInt("AppSettings:LogRetentionDays", 30);
    }

    private static T GetSection<T>(string section, string key, T defaultValue)
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return defaultValue;

            var json = File.ReadAllText(_settingsPath);
            var node = JsonNode.Parse(json);
            var value = node?[section]?[key];
            if (value == null)
                return defaultValue;

            return value.GetValue<T>();
        }
        catch
        {
            return defaultValue;
        }
    }

    private static bool GetBool(string path, bool defaultValue)
    {
        var parts = path.Split(':');
        return parts.Length == 2
            ? GetSection<bool>(parts[0], parts[1], defaultValue)
            : defaultValue;
    }

    private static int GetInt(string path, int defaultValue)
    {
        var parts = path.Split(':');
        return parts.Length == 2
            ? GetSection<int>(parts[0], parts[1], defaultValue)
            : defaultValue;
    }

    private static void SetValue(string section, string key, object value)
    {
        try
        {
            // Ensure directories exist even if called early.
            MedProject.Utilities.AppPaths.EnsureUserFiles();

            JsonNode root;
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                root = JsonNode.Parse(json) ?? new JsonObject();
            }
            else
            {
                root = new JsonObject();
            }

            root[section] ??= new JsonObject();
            root[section]![key] = JsonValue.Create(value);

            File.WriteAllText(_settingsPath, root.ToJsonString(_options));
        }
        catch { }
    }

    public void Save() { }
}
