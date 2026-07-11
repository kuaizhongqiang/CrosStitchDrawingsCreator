using System.IO;
using System.Text.Json;
using CrosStitchDrawingsCreator.Models;

namespace CrosStitchDrawingsCreator.Services;

/// <summary>
/// Loads and queries the built-in DMC thread color library.
/// </summary>
public class DmcService
{
    private List<DmcColor> _colors = [];

    /// <summary>
    /// Load the DMC color library from an embedded JSON resource.
    /// </summary>
    public async Task LoadLibraryAsync(string jsonPath)
    {
        var json = await File.ReadAllTextAsync(jsonPath);
        var library = JsonSerializer.Deserialize<DmcColorLibrary>(json);
        _colors = library?.Colors ?? [];
    }

    /// <summary>
    /// Load the DMC color library from a stream (embedded resource).
    /// </summary>
    public async Task LoadLibraryAsync(Stream jsonStream)
    {
        using var reader = new StreamReader(jsonStream);
        var json = await reader.ReadToEndAsync();
        var library = JsonSerializer.Deserialize<DmcColorLibrary>(json);
        _colors = library?.Colors ?? [];
    }

    /// <summary>
    /// Find the nearest DMC color for an RGB value using Euclidean distance.
    /// </summary>
    public (DmcColor color, double distance) FindNearest(byte r, byte g, byte b)
    {
        if (_colors.Count == 0)
            throw new InvalidOperationException("DMC color library not loaded.");

        DmcColor nearest = _colors[0];
        double minDist = double.MaxValue;

        foreach (var dmc in _colors)
        {
            double dr = r - dmc.Rgb.R;
            double dg = g - dmc.Rgb.G;
            double db = b - dmc.Rgb.B;
            double dist = dr * dr + dg * dg + db * db;

            if (dist < minDist)
            {
                minDist = dist;
                nearest = dmc;
            }
        }

        return (nearest, Math.Sqrt(minDist));
    }

    /// <summary>
    /// Map a palette to nearest DMC colors.
    /// </summary>
    public void MapPalette(List<PaletteEntry> palette)
    {
        foreach (var entry in palette)
        {
            var (dmc, _) = FindNearest(entry.R, entry.G, entry.B);
            entry.DmcCode = dmc.DmcCode;
            entry.DmcName = dmc.Name;
        }
    }

    /// <summary>Number of loaded DMC colors.</summary>
    public int Count => _colors.Count;
}
