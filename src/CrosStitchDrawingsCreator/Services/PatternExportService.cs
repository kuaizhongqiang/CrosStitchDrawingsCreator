using System.IO;
using CrosStitchDrawingsCreator.Models;
using SkiaSharp;

namespace CrosStitchDrawingsCreator.Services;

/// <summary>
/// Exports a pattern to PNG with configurable scale, grid lines, and legend.
/// </summary>
public class PatternExportService
{
    /// <summary>
    /// Export pattern as PNG with the given settings.
    /// </summary>
    public void ExportToPng(PatternData pattern, ExportSettings settings, string outputPath)
    {
        int scale = Math.Clamp(settings.ScaleFactor, 1, 10);
        int stitchSize = scale;
        int gridLineSize = settings.IncludeGridLines ? 1 : 0;

        int gridWidth = pattern.Width;
        int gridHeight = pattern.Height;

        // Calculate dimensions
        int pixelWidth = gridWidth * stitchSize + (gridWidth + 1) * gridLineSize;
        int pixelHeight = gridHeight * stitchSize + (gridHeight + 1) * gridLineSize;

        int legendHeight = 0;
        if (settings.IncludeLegend)
        {
            legendHeight = 40 + pattern.Palette.Count * 30 + 20;
        }

        using var surface = SKSurface.Create(new SKImageInfo(pixelWidth, pixelHeight + legendHeight));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        // Draw grid cells
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                int idx = pattern.PixelGrid[y, x];
                var color = idx >= 0 && idx < pattern.Palette.Count
                    ? new SKColor(pattern.Palette[idx].R, pattern.Palette[idx].G, pattern.Palette[idx].B)
                    : SKColors.White;

                int px = x * stitchSize + (x + 1) * gridLineSize;
                int py = y * stitchSize + (y + 1) * gridLineSize;

                using var paint = new SKPaint { Color = color, Style = SKPaintStyle.Fill };
                canvas.DrawRect(px, py, stitchSize, stitchSize, paint);
            }
        }

        // Draw grid lines
        if (settings.IncludeGridLines)
        {
            using var gridPaint = new SKPaint { Color = SKColors.LightGray, Style = SKPaintStyle.Fill };

            // Horizontal lines
            for (int y = 0; y <= gridHeight; y++)
            {
                int py = y * stitchSize + y * gridLineSize;
                canvas.DrawRect(0, py, pixelWidth, gridLineSize, gridPaint);
            }

            // Vertical lines
            for (int x = 0; x <= gridWidth; x++)
            {
                int px = x * stitchSize + x * gridLineSize;
                canvas.DrawRect(px, 0, gridLineSize, pixelHeight, gridPaint);
            }
        }

        // Draw legend
        if (settings.IncludeLegend && pattern.Palette.Count > 0)
        {
            DrawLegend(canvas, pattern, pixelWidth, pixelHeight);
        }

        // Save to PNG
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var fileStream = File.OpenWrite(outputPath);
        data.SaveTo(fileStream);
    }

    private static void DrawLegend(SKCanvas canvas, PatternData pattern, int imageWidth, int startY)
    {
        int y = startY + 10;
        int swatchSize = 20;
        int margin = 20;

        using var titleFont = new SKFont(SKTypeface.FromFamilyName("Microsoft YaHei"), 14);
        using var textFont = new SKFont(SKTypeface.FromFamilyName("Microsoft YaHei"), 11);
        using var titlePaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
        using var textPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };

        // Title
        canvas.DrawText("颜色图例 / Color Legend", margin, y + 14, SKTextAlign.Left, titleFont, titlePaint);
        y += 30;

        foreach (var entry in pattern.Palette)
        {
            // Color swatch
            using var swatchPaint = new SKPaint
            {
                Color = new SKColor(entry.R, entry.G, entry.B),
                Style = SKPaintStyle.Fill,
                StrokeWidth = 1
            };
            // Draw swatch with border
            var rect = new SKRect(margin, y, margin + swatchSize, y + swatchSize);
            canvas.DrawRect(rect, swatchPaint);
            using var borderPaint = new SKPaint { Color = SKColors.LightGray, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
            canvas.DrawRect(rect, borderPaint);

            // Label
            string label = entry.DmcCode != null
                ? $"DMC {entry.DmcCode}  {entry.Hex}  {entry.StitchCount} 针"
                : $"{entry.Hex}  {entry.StitchCount} 针";
            canvas.DrawText(label, margin + swatchSize + 8, y + 15, SKTextAlign.Left, textFont, textPaint);

            y += 28;
        }
    }
}
