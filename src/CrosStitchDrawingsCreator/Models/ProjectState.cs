namespace CrosStitchDrawingsCreator.Models;

/// <summary>
/// Complete application state: source image, parameters, generated pattern, export settings.
/// </summary>
public class ProjectState
{
    /// <summary>Source image info (null before import).</summary>
    public SourceImageInfo? SourceImage { get; set; }

    /// <summary>User-configured generation parameters.</summary>
    public GenerationParameters Parameters { get; set; } = new();

    /// <summary>Generated pattern (null before generation).</summary>
    public PatternData? GeneratedPattern { get; set; }

    /// <summary>Export configuration.</summary>
    public ExportSettings ExportSettings { get; set; } = new();

    /// <summary>Whether a pattern has been generated.</summary>
    public bool HasPattern => GeneratedPattern != null;
}

public class SourceImageInfo
{
    public string FilePath { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSizeBytes { get; set; }
    public int OriginalColorCount { get; set; }

    public string FileSizeDisplay => FileSizeBytes switch
    {
        < 1024 => $"{FileSizeBytes} B",
        < 1024 * 1024 => $"{FileSizeBytes / 1024.0:F1} KB",
        _ => $"{FileSizeBytes / (1024.0 * 1024.0):F1} MB"
    };
}

public class GenerationParameters
{
    private int _colorCount = 30;
    private int _width = 150;
    private int _height = 150;

    /// <summary>Number of thread colors to use (2-200).</summary>
    public int ColorCount
    {
        get => _colorCount;
        set => _colorCount = Math.Clamp(value, 2, 200);
    }

    /// <summary>Pattern width in stitches (10-500).</summary>
    public int Width
    {
        get => _width;
        set => _width = Math.Clamp(value, 10, 500);
    }

    /// <summary>Pattern height in stitches (10-500).</summary>
    public int Height
    {
        get => _height;
        set => _height = Math.Clamp(value, 10, 500);
    }

    /// <summary>Lock aspect ratio to original image.</summary>
    public bool AspectRatioLocked { get; set; } = true;

    /// <summary>Show grid lines on preview.</summary>
    public bool ShowGridLines { get; set; } = true;

    /// <summary>Grid line thickness in pixels (1-3).</summary>
    public int GridLineThickness { get; set; } = 1;

    /// <summary>Grid line color (Black, White, Gray).</summary>
    public GridLineColor GridLineColor { get; set; } = GridLineColor.Gray;

    /// <summary>Apply Floyd-Steinberg dithering.</summary>
    public bool EnableDithering { get; set; }
}

public enum GridLineColor { Black, White, Gray }

public class ExportSettings
{
    /// <summary>Export scale factor (1-10).</summary>
    public int ScaleFactor { get; set; } = 4;

    /// <summary>Include grid lines in export.</summary>
    public bool IncludeGridLines { get; set; } = true;

    /// <summary>Append color legend below the pattern image.</summary>
    public bool IncludeLegend { get; set; } = true;

    /// <summary>Last used export directory.</summary>
    public string? LastExportPath { get; set; }
}
