using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CrosStitchDrawingsCreator.Services;
using CrosStitchDrawingsCreator.ViewModels;

namespace CrosStitchDrawingsCreator;

public partial class MainWindow : Window
{
    private double _zoomFactor = 1.0;
    private const double ZoomMin = 0.25;
    private const double ZoomMax = 8.0;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Restore window state from settings
        var settings = App.SettingsService.Settings;
        if (settings.WindowLeft >= 0 && settings.WindowTop >= 0)
        {
            Left = settings.WindowLeft;
            Top = settings.WindowTop;
        }
        Width = settings.WindowWidth;
        Height = settings.WindowHeight;

        if (settings.WindowMaximized)
            WindowState = WindowState.Maximized;

        // Restore last export path
        if (DataContext is MainViewModel vm && !string.IsNullOrEmpty(settings.LastExportPath))
            vm.ExportPath = settings.LastExportPath;
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Save window state
        App.SettingsService.SaveWindowState(
            Left, Top, Width, Height,
            WindowState == WindowState.Maximized);
    }

    // ── Window Controls ──

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // ── Drag to Move ──
    private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    // ── Image Drag-Drop ──
    private void OnImageDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;

            var dropZone = (Border)sender;
            dropZone.Background = new SolidColorBrush(Color.FromArgb(20, 0, 120, 212));
            dropZone.BorderBrush = FindResource("PrimaryBrush") as Brush;
        }
    }

    private void OnImageDragLeave(object sender, DragEventArgs e)
    {
        var dropZone = (Border)sender;
        dropZone.Background = FindResource("SurfaceBrush") as Brush;
        dropZone.BorderBrush = FindResource("BorderBrush") as Brush;
    }

    private void OnImageDrop(object sender, DragEventArgs e)
    {
        var dropZone = (Border)sender;
        dropZone.Background = FindResource("SurfaceBrush") as Brush;
        dropZone.BorderBrush = FindResource("BorderBrush") as Brush;

        if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
            DataContext is MainViewModel vm)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                // Trigger file import via ViewModel
                vm.ImportImageCommand.Execute(null);
            }
        }
    }

    // ── Color Legend Click (Highlight) ──
    private void OnLegendItemClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is Models.PaletteEntry entry
            && DataContext is ViewModels.MainViewModel vm)
        {
            var index = vm.PaletteEntries.IndexOf(entry);
            vm.SelectColorCommand.Execute(index);
        }
    }

    // ── Mouse Wheel Zoom ──
    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.HasPattern)
        {
            var image = (System.Windows.Controls.Image)sender;
            double delta = e.Delta > 0 ? 1.1 : 1.0 / 1.1;
            _zoomFactor = Math.Clamp(_zoomFactor * delta, ZoomMin, ZoomMax);

            image.LayoutTransform = new ScaleTransform(_zoomFactor, _zoomFactor);
            e.Handled = true;
        }
    }
}
