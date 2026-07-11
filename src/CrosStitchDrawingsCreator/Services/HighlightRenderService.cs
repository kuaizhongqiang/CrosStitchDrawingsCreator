using CrosStitchDrawingsCreator.Models;
using SkiaSharp;

namespace CrosStitchDrawingsCreator.Services;

/// <summary>
/// Renders pattern preview with optional color highlighting and symbol overlays.
/// </summary>
public class HighlightRenderService
{
    /// <summary>
    /// Render pattern preview bitmap.
    /// </summary>
    /// <param name="pattern">Pattern data.</param>
    /// <param name="showGrid">Whether to show grid lines.</param>
    /// <param name="gridThickness">Grid line thickness (1-3).</param>
    /// <param name="scale">Pixel scale per stitch.</param>
    /// <param name="highlightColorIndex">Color index to highlight, or -1 for none.</param>
    /// <param name="showSymbols">Whether to overlay cross-stitch symbols.</param>
    /// <returns>SKBitmap of the rendered preview.</returns>
    public SKBitmap RenderPreview(
        PatternData pattern,
        bool showGrid,
        int gridThickness,
        int scale,
        int highlightColorIndex,
        bool showSymbols)
    {
        int stitchSize = Math.Max(1, scale);
        int gridLineSize = showGrid ? gridThickness : 0;

        int pixelWidth = pattern.Width * stitchSize + (pattern.Width + 1) * gridLineSize;
        int pixelHeight = pattern.Height * stitchSize + (pattern.Height + 1) * gridLineSize;

        var bitmap = new SKBitmap(pixelWidth, pixelHeight);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        // Draw cells
        for (int y = 0; y < pattern.Height; y++)
        {
            for (int x = 0; x < pattern.Width; x++)
            {
                int idx = pattern.PixelGrid[y, x];
                var baseColor = idx >= 0 && idx < pattern.Palette.Count
                    ? new SKColor(pattern.Palette[idx].R, pattern.Palette[idx].G, pattern.Palette[idx].B)
                    : SKColors.White;

                // Highlight mode: dim non-highlighted colors
                SKColor drawColor;
                if (highlightColorIndex >= 0 && idx != highlightColorIndex)
                {
                    // Dim other colors
                    drawColor = new SKColor(
                        (byte)(baseColor.Red / 3 + 170),
                        (byte)(baseColor.Green / 3 + 170),
                        (byte)(baseColor.Blue / 3 + 170));
                }
                else
                {
                    drawColor = baseColor;
                }

                int px = x * stitchSize + (x + 1) * gridLineSize;
                int py = y * stitchSize + (y + 1) * gridLineSize;

                using var paint = new SKPaint { Color = drawColor, Style = SKPaintStyle.Fill };
                canvas.DrawRect(px, py, stitchSize, stitchSize, paint);

                // Draw cross-stitch symbol overlay
                if (showSymbols && stitchSize >= 4)
                {
                    DrawSymbol(canvas, px, py, stitchSize, idx);
                }
            }
        }

        // Grid lines
        if (showGrid)
        {
            using var gridPaint = new SKPaint { Color = SKColors.LightGray, Style = SKPaintStyle.Fill };
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

        return bitmap;
    }

    private static void DrawSymbol(SKCanvas canvas, int px, int py, int size, int colorIndex)
    {
        // Simple symbols based on color index (rotate through a set)
        string[] symbols = ["×", "+", "/", "\\", "•", "♦", "★", "○"];
        var symbol = symbols[colorIndex % symbols.Length];

        using var font = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), size * 0.7f);
        using var paint = new SKPaint
        {
            Color = GetContrastColor(canvas, px, py, size),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawText(symbol, px + size * 0.15f, py + size * 0.85f, SKTextAlign.Left, font, paint);
    }

    private static SKColor GetContrastColor(SKCanvas canvas, int px, int py, int size)
    {
        // Sample center pixel to determine contrast color
        // Simple heuristic: light background → dark symbol, dark background → light symbol
        return SKColors.Black; // Default to black for simplicity
    }
}
