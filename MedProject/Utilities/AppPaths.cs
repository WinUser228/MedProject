using System;
using System.IO;

namespace MedProject.Utilities;

public static class AppPaths
{
    public const string AppName = "MedProject";

    /// <summary>
    /// Writable app data directory (Windows: %LocalAppData%\MedProject, Linux: ~/.local/share/MedProject).
    /// </summary>
    public static string DataDir
    {
        get
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(root))
                root = AppContext.BaseDirectory; // fallback; may be read-only on Linux packages
            return Path.Combine(root, AppName);
        }
    }

    /// <summary>
    /// Writable config directory (Windows: %AppData%\MedProject, Linux: ~/.config/MedProject).
    /// </summary>
    public static string ConfigDir
    {
        get
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrWhiteSpace(root))
                root = DataDir;
            return Path.Combine(root, AppName);
        }
    }

    public static string DatabasePath => Path.Combine(DataDir, "HospitalStatistics.db");
    public static string SettingsPath => Path.Combine(ConfigDir, "appsettings.json");

    public static void EnsureUserFiles()
    {
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(ConfigDir);

        // If settings don't exist in user profile, seed from app folder (if present).
        if (!File.Exists(SettingsPath))
        {
            var bundled = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(bundled))
            {
                File.Copy(bundled, SettingsPath, overwrite: false);
            }
        }
    }
}

