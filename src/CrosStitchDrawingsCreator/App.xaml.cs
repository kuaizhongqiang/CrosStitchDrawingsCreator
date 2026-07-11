using System.Windows;
using CrosStitchDrawingsCreator.Services;

namespace CrosStitchDrawingsCreator;

public partial class App : Application
{
    public static ThemeService ThemeService { get; } = new();
    public static SettingsService SettingsService { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Load persisted settings
        var settings = SettingsService.Load();

        // Apply saved theme
        ThemeService.SetTheme(settings.Theme);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SettingsService.Save();
        base.OnExit(e);
    }
}
