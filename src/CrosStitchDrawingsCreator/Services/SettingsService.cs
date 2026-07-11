using System.IO;
using System.Text.Json;

namespace CrosStitchDrawingsCreator.Services;

/// <summary>
/// Persists user settings to %APPDATA%/CrosStitchDrawingsCreator/settings.json.
/// </summary>
public class SettingsService
{
    private readonly string _settingsDir;
    private readonly string _settingsPath;
    private AppSettings _settings = new();

    public SettingsService()
    {
        _settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CrosStitchDrawingsCreator");
        _settingsPath = Path.Combine(_settingsDir, "settings.json");
    }

    /// <summary>Current settings (in-memory).</summary>
    public AppSettings Settings => _settings;

    /// <summary>Load settings from disk, or create defaults.</summary>
    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            _settings = new AppSettings();
        }
        return _settings;
    }

    /// <summary>Save current settings to disk.</summary>
    public void Save()
    {
        try
        {
            if (!Directory.Exists(_settingsDir))
                Directory.CreateDirectory(_settingsDir);

            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Silently fail — settings save is non-critical
        }
    }

    /// <summary>Update and persist window state.</summary>
    public void SaveWindowState(double left, double top, double width, double height, bool maximized)
    {
        _settings.WindowLeft = left;
        _settings.WindowTop = top;
        _settings.WindowWidth = width;
        _settings.WindowHeight = height;
        _settings.WindowMaximized = maximized;
        Save();
    }

    /// <summary>Update and persist theme choice.</summary>
    public void SaveTheme(string theme)
    {
        _settings.Theme = theme;
        Save();
    }

    /// <summary>Update and persist export path.</summary>
    public void SaveExportPath(string path)
    {
        _settings.LastExportPath = path;
        Save();
    }
}

public class AppSettings
{
    public string Theme { get; set; } = "System";  // System, Light, Dark
    public string? LastExportPath { get; set; }
    public double WindowLeft { get; set; } = -1;
    public double WindowTop { get; set; } = -1;
    public double WindowWidth { get; set; } = 1200;
    public double WindowHeight { get; set; } = 800;
    public bool WindowMaximized { get; set; }
    public int ColorCount { get; set; } = 30;
    public int PatternWidth { get; set; } = 150;
    public int PatternHeight { get; set; } = 150;
    public bool AspectRatioLocked { get; set; } = true;
    public bool ShowGridLines { get; set; } = true;
    public int GridLineThickness { get; set; } = 1;
    public int ExportScale { get; set; } = 4;
    public bool ExportIncludeGridLines { get; set; } = true;
    public bool ExportIncludeLegend { get; set; } = true;
}
