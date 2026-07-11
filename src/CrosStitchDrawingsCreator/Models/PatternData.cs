namespace CrosStitchDrawingsCreator.Models;

/// <summary>
/// Represents the complete generated cross-stitch pattern.
/// </summary>
public class PatternData
{
    /// <summary>Color index for each stitch cell [row, col].</summary>
    public int[,] PixelGrid { get; set; } = new int[0, 0];

    /// <summary>Ordered palette: index → color info.</summary>
    public List<PaletteEntry> Palette { get; set; } = [];

    /// <summary>Width in stitches.</summary>
    public int Width { get; set; }

    /// <summary>Height in stitches.</summary>
    public int Height { get; set; }

    /// <summary>Original source image path.</summary>
    public string? SourceImagePath { get; set; }

    /// <summary>Original source image dimensions.</summary>
    public (int Width, int Height) SourceDimensions { get; set; }
}

/// <summary>
/// One entry in the color palette: color value, DMC match, stitch count.
/// </summary>
public class PaletteEntry
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }

    public string Hex => $"#{R:X2}{G:X2}{B:X2}";

    /// <summary>DMC thread code (e.g. "310"), or null if not mapped.</summary>
    public string? DmcCode { get; set; }

    /// <summary>DMC color name.</summary>
    public string? DmcName { get; set; }

    /// <summary>Number of stitches using this color.</summary>
    public int StitchCount { get; set; }
}
