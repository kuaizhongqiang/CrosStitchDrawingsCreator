using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CrosStitchDrawingsCreator.Models;
using CrosStitchDrawingsCreator.Services;
using Microsoft.Win32;

namespace CrosStitchDrawingsCreator.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ImageProcessingService _imageProcessing = new();
    private readonly PatternExportService _exportService = new();
    private readonly DmcService _dmcService = new();

    private string? _loadedImagePath;

    // ── Imported image info ──
    [ObservableProperty]
    private BitmapSource? _thumbnailImage;

    [ObservableProperty]
    private string _imageInfoText = string.Empty;

    [ObservableProperty]
    private bool _hasImage;

    // ── Parameters ──
    [ObservableProperty]
    private int _colorCount = 30;

    [ObservableProperty]
    private int _patternWidth = 150;

    [ObservableProperty]
    private int _patternHeight = 150;

    [ObservableProperty]
    private bool _aspectRatioLocked = true;

    [ObservableProperty]
    private bool _showGridLines = true;

    [ObservableProperty]
    private int _gridLineThickness = 1;

    [ObservableProperty]
    private bool _enableDithering;

    // ── Pattern ──
    [ObservableProperty]
    private BitmapSource? _patternPreview;

    [ObservableProperty]
    private bool _hasPattern;

    [ObservableProperty]
    private ObservableCollection<PaletteEntry> _paletteEntries = [];

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private int _paletteCount;

    // ── Export ──
    [ObservableProperty]
    private int _exportScale = 4;

    [ObservableProperty]
    private bool _exportIncludeGridLines = true;

    [ObservableProperty]
    private bool _exportIncludeLegend = true;

    [ObservableProperty]
    private string _exportPath = string.Empty;

    // Stored pattern data for export
    private PatternData? _currentPattern;
    private GenerationParameters _currentParams = new();

    private double _aspectRatio = 1.0;

    public MainViewModel()
    {
        // Set default export path to Desktop/CrossStitchPatterns
        var defaultDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CrossStitchPatterns");
        ExportPath = defaultDir;
    }

    // ── Commands ──

    [RelayCommand]
    private void ImportImage()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff;*.tif;*.webp|所有文件|*.*",
            Title = "选择要转换的图片"
        };

        if (dialog.ShowDialog() == true)
        {
            LoadImage(dialog.FileName);
        }
    }

    [RelayCommand]
    private Task PasteImage()
    {
        if (Clipboard.ContainsImage())
        {
            var clipboardImage = Clipboard.GetImage();
            if (clipboardImage != null)
            {
                // Save clipboard image temporarily
                var tempDir = Path.GetTempPath();
                var tempPath = Path.Combine(tempDir, "crosstitch_clipboard.png");
                using var fileStream = File.OpenWrite(tempPath);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(clipboardImage));
                encoder.Save(fileStream);

                LoadImage(tempPath);
            }
        }
        return Task.CompletedTask;
    }

    private void LoadImage(string filePath)
    {
        try
        {
            var info = _imageProcessing.LoadImageInfo(filePath);
            _loadedImagePath = filePath;

            // Load thumbnail
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filePath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 200; // thumbnail size
            bitmap.EndInit();
            bitmap.Freeze();

            ThumbnailImage = bitmap;
            HasImage = true;

            // Store aspect ratio
            _aspectRatio = (double)info.Width / info.Height;

            ImageInfoText = $"{info.Width}×{info.Height}  |  {info.FileSizeDisplay}  |  原始颜色: {info.OriginalColorCount}";

            // Auto-set pattern dimensions based on image
            PatternWidth = Math.Clamp(info.Width, 10, 500);
            PatternHeight = Math.Clamp(info.Height, 10, 500);

            // Clear previous pattern
            HasPattern = false;
            PatternPreview = null;
            _currentPattern = null;
            PaletteEntries.Clear();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法加载图片: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    partial void OnPatternWidthChanged(int value)
    {
        if (AspectRatioLocked && HasImage && value > 0)
        {
            var newHeight = (int)Math.Round(value / _aspectRatio);
            if (newHeight != PatternHeight)
            {
                _patternHeight = Math.Clamp(newHeight, 10, 500);
                OnPropertyChanged(nameof(PatternHeight));
            }
        }
    }

    partial void OnPatternHeightChanged(int value)
    {
        if (AspectRatioLocked && HasImage && value > 0)
        {
            var newWidth = (int)Math.Round(value * _aspectRatio);
            if (newWidth != PatternWidth)
            {
                _patternWidth = Math.Clamp(newWidth, 10, 500);
                OnPropertyChanged(nameof(PatternWidth));
            }
        }
    }

    [RelayCommand]
    private async Task GeneratePattern()
    {
        if (!HasImage || string.IsNullOrEmpty(_loadedImagePath))
        {
            MessageBox.Show("请先导入图片。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        IsGenerating = true;
        HasPattern = false;

        try
        {
            var parameters = new GenerationParameters
            {
                ColorCount = ColorCount,
                Width = PatternWidth,
                Height = PatternHeight,
                AspectRatioLocked = AspectRatioLocked,
                ShowGridLines = ShowGridLines,
                GridLineThickness = GridLineThickness,
                EnableDithering = EnableDithering
            };

            _currentParams = parameters;

            // Run processing in background
            var bitmap = await Task.Run(() => _imageProcessing.LoadBitmap(_loadedImagePath));
            var pattern = await Task.Run(() => _imageProcessing.GeneratePattern(bitmap, parameters));

            _currentPattern = pattern;

            // Load DMC library from embedded resource
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(n => n.Contains("dmc_colors"));
                if (resourceName != null)
                {
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        await _dmcService.LoadLibraryAsync(stream);
                        _dmcService.MapPalette(pattern.Palette);
                    }
                }
            }
            catch
            {
                // DMC mapping is optional in MVP
            }

            // Render preview on UI thread
            await Task.Run(() => RenderPreview(pattern, parameters));

            PaletteEntries = new ObservableCollection<PaletteEntry>(pattern.Palette);
            PaletteCount = pattern.Palette.Count;
            HasPattern = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"生成图纸失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private void RenderPreview(PatternData pattern, GenerationParameters parameters)
    {
        int scale = Math.Max(1, Math.Min(800 / pattern.Width, 600 / pattern.Height));
        if (scale < 1) scale = 1;

        int stitchSize = scale;
        int gridLineSize = parameters.ShowGridLines ? parameters.GridLineThickness : 0;

        int pixelWidth = pattern.Width * stitchSize + (pattern.Width + 1) * gridLineSize;
        int pixelHeight = pattern.Height * stitchSize + (pattern.Height + 1) * gridLineSize;

        // Use SkiaSharp to render
        using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(pixelWidth, pixelHeight));
        var canvas = surface.Canvas;
        canvas.Clear(SkiaSharp.SKColors.White);

        for (int y = 0; y < pattern.Height; y++)
        {
            for (int x = 0; x < pattern.Width; x++)
            {
                int idx = pattern.PixelGrid[y, x];
                var color = idx >= 0 && idx < pattern.Palette.Count
                    ? new SkiaSharp.SKColor(pattern.Palette[idx].R, pattern.Palette[idx].G, pattern.Palette[idx].B)
                    : SkiaSharp.SKColors.White;

                int px = x * stitchSize + (x + 1) * gridLineSize;
                int py = y * stitchSize + (y + 1) * gridLineSize;

                using var paint = new SkiaSharp.SKPaint { Color = color, Style = SkiaSharp.SKPaintStyle.Fill };
                canvas.DrawRect(px, py, stitchSize, stitchSize, paint);
            }
        }

        // Grid lines
        if (parameters.ShowGridLines)
        {
            var gridColor = parameters.GridLineThickness switch
            {
                1 => SkiaSharp.SKColors.LightGray,
                2 => SkiaSharp.SKColors.Gray,
                _ => SkiaSharp.SKColors.DarkGray
            };
            using var gridPaint = new SkiaSharp.SKPaint { Color = gridColor, Style = SkiaSharp.SKPaintStyle.Fill };

            for (int y = 0; y <= pattern.Height; y++)
            {
                int py = y * stitchSize + y * gridLineSize;
                canvas.DrawRect(0, py, pixelWidth, gridLineSize, gridPaint);
            }
            for (int x = 0; x <= pattern.Width; x++)
            {
                int px = x * stitchSize + x * gridLineSize;
                canvas.DrawRect(px, 0, gridLineSize, pixelHeight, gridPaint);
            }
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        var ms = new System.IO.MemoryStream(data.ToArray());
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.StreamSource = ms;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();

        Application.Current.Dispatcher.Invoke(() => PatternPreview = bitmap);
    }

    [RelayCommand]
    private void ExportPng()
    {
        if (_currentPattern == null)
        {
            MessageBox.Show("请先生成图纸。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var defaultName = $"{Path.GetFileNameWithoutExtension(_loadedImagePath ?? "pattern")}_{PatternWidth}x{PatternHeight}_{ColorCount}colors.png";
        var dialog = new SaveFileDialog
        {
            Filter = "PNG 图片|*.png",
            Title = "导出图纸",
            FileName = defaultName,
            InitialDirectory = ExportPath
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var settings = new ExportSettings
                {
                    ScaleFactor = ExportScale,
                    IncludeGridLines = ExportIncludeGridLines,
                    IncludeLegend = ExportIncludeLegend
                };

                _exportService.ExportToPng(_currentPattern, settings, dialog.FileName);
                ExportPath = Path.GetDirectoryName(dialog.FileName) ?? ExportPath;

                MessageBox.Show($"图纸已导出至:\n{dialog.FileName}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private void SelectExportPath()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择导出目录",
            DefaultDirectory = ExportPath
        };

        if (dialog.ShowDialog() == true)
        {
            ExportPath = dialog.FolderName;
        }
    }
}
