using System.Windows;

namespace CrosStitchDrawingsCreator.Services;

/// <summary>
/// Manages light/dark theme switching at runtime.
/// </summary>
public class ThemeService
{
    private const string BaseThemeKey = "ThemeBase";
    private const string DarkOverlayKey = "ThemeDarkOverlay";

    /// <summary>Current theme mode.</summary>
    public string CurrentTheme { get; private set; } = "System";

    /// <summary>Switch theme and update application resources.</summary>
    public void SetTheme(string theme)
    {
        CurrentTheme = theme;

        var app = Application.Current;
        if (app == null) return;

        // Remove existing theme overlay
        var toRemove = app.Resources.MergedDictionaries
            .Where(d => d.Contains(DarkOverlayKey))
            .ToList();
        foreach (var d in toRemove)
            app.Resources.MergedDictionaries.Remove(d);

        // Apply dark theme if selected
        bool useDark = theme switch
        {
            "Dark" => true,
            "Light" => false,
            _ => IsSystemDarkMode()
        };

        if (useDark)
        {
            var darkDict = new ResourceDictionary
            {
                Source = new Uri("/Themes/DarkTheme.xaml", UriKind.Relative)
            };
            darkDict[DarkOverlayKey] = true;
            app.Resources.MergedDictionaries.Add(darkDict);
        }
    }

    /// <summary>Detect Windows dark mode setting.</summary>
    private static bool IsSystemDarkMode()
    {
        try
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int value)
                return value == 0;
        }
        catch { }
        return false;
    }

    /// <summary>Toggle between light and dark.</summary>
    public string Toggle()
    {
        var next = CurrentTheme switch
        {
            "Light" => "Dark",
            "Dark" => "Light",
            _ => IsSystemDarkMode() ? "Light" : "Dark"
        };
        SetTheme(next);
        return next;
    }
}
