using System.IO;
using CrosStitchDrawingsCreator.Models;
using SkiaSharp;

namespace CrosStitchDrawingsCreator.Services;

/// <summary>
/// Core image processing pipeline: load, resize, quantize, build pattern.
/// </summary>
public class ImageProcessingService
{
    /// <summary>
    /// Load image from file path and return source info.
    /// </summary>
    public SourceImageInfo LoadImageInfo(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var codec = SKCodec.Create(stream);
        var info = codec.Info;
        return new SourceImageInfo
        {
            FilePath = filePath,
            Width = info.Width,
            Height = info.Height,
            FileSizeBytes = new FileInfo(filePath).Length,
            OriginalColorCount = 0 // computed during quantization
        };
    }

    /// <summary>
    /// Load bitmap from file.
    /// </summary>
    public SKBitmap LoadBitmap(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return SKBitmap.Decode(stream);
    }

    /// <summary>
    /// Generate a cross-stitch pattern: resize + quantize + build pixel grid.
    /// </summary>
    public PatternData GeneratePattern(SKBitmap sourceBitmap, GenerationParameters parameters)
    {
        // 1. Resize to target stitch dimensions using high-quality sampling
        using var resized = sourceBitmap.Resize(
            new SKImageInfo(parameters.Width, parameters.Height),
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));

        if (resized == null)
            throw new InvalidOperationException("Failed to resize image.");

        // 2. Extract pixels
        var pixels = new SKColor[parameters.Height, parameters.Width];
        for (int y = 0; y < parameters.Height; y++)
        for (int x = 0; x < parameters.Width; x++)
            pixels[y, x] = resized.GetPixel(x, y);

        // 3. Quantize colors using K-Means
        var (quantizedPixels, palette) = QuantizeKMeans(pixels, parameters.ColorCount, parameters.EnableDithering);

        // 4. Build PatternData
        var pattern = new PatternData
        {
            Width = parameters.Width,
            Height = parameters.Height,
            PixelGrid = quantizedPixels,
            Palette = palette.Select((c, i) => new PaletteEntry
            {
                R = c.Red,
                G = c.Green,
                B = c.Blue,
                StitchCount = 0
            }).ToList()
        };

        // 5. Count stitches per color
        for (int y = 0; y < pattern.Height; y++)
        for (int x = 0; x < pattern.Width; x++)
        {
            int idx = pattern.PixelGrid[y, x];
            if (idx >= 0 && idx < pattern.Palette.Count)
                pattern.Palette[idx].StitchCount++;
        }

        return pattern;
    }

    /// <summary>
    /// K-Means color quantization. Returns (pixelGrid, palette).
    /// </summary>
    private (int[,] pixelGrid, List<SKColor> palette) QuantizeKMeans(
        SKColor[,] pixels, int k, bool dither)
    {
        int height = pixels.GetLength(0);
        int width = pixels.GetLength(1);
        int totalPixels = height * width;

        // Flatten pixels to float vectors
        var samples = new List<SKColor>();
        foreach (var p in pixels)
            samples.Add(p);

        // Initialize K-Means with uniform sampling
        var centroids = InitializeCentroids(samples, k);
        var assignments = new int[totalPixels];

        // Iterate K-Means (max 50 iterations)
        for (int iter = 0; iter < 50; iter++)
        {
            // Assign each pixel to nearest centroid
            bool changed = false;
            for (int i = 0; i < totalPixels; i++)
            {
                int nearest = FindNearestCentroid(samples[i], centroids);
                if (assignments[i] != nearest)
                {
                    assignments[i] = nearest;
                    changed = true;
                }
            }

            if (!changed) break;

            // Recompute centroids
            var newCentroids = new List<Centroid>();
            for (int c = 0; c < k; c++)
            {
                int count = 0;
                long sumR = 0, sumG = 0, sumB = 0;
                for (int i = 0; i < totalPixels; i++)
                {
                    if (assignments[i] == c)
                    {
                        sumR += samples[i].Red;
                        sumG += samples[i].Green;
                        sumB += samples[i].Blue;
                        count++;
                    }
                }

                if (count > 0)
                {
                    newCentroids.Add(new Centroid
                    {
                        Color = new SKColor(
                            (byte)(sumR / count),
                            (byte)(sumG / count),
                            (byte)(sumB / count)),
                    });
                }
                else
                {
                    // Keep old centroid if no pixels assigned
                    newCentroids.Add(centroids[c]);
                }
            }
            centroids = newCentroids;
        }

        // Build result
        var pixelGrid = new int[height, width];
        var palette = centroids.Select(c => c.Color).ToList();

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int idx = y * width + x;
            pixelGrid[y, x] = assignments[idx];
        }

        // Apply Floyd-Steinberg dithering if enabled
        if (dither)
        {
            ApplyFloydSteinberg(pixels, pixelGrid, palette, height, width);
        }

        return (pixelGrid, palette);
    }

    private List<Centroid> InitializeCentroids(List<SKColor> samples, int k)
    {
        // K-Means++ initialization
        var random = new Random(42);
        var centroids = new List<Centroid>();

        // Pick first centroid randomly
        centroids.Add(new Centroid { Color = samples[random.Next(samples.Count)] });

        for (int c = 1; c < k; c++)
        {
            // Compute distances to nearest centroid
            var distances = samples.Select(s =>
            {
                double minDist = double.MaxValue;
                foreach (var cent in centroids)
                {
                    double d = ColorDistance(s, cent.Color);
                    if (d < minDist) minDist = d;
                }
                return minDist;
            }).ToArray();

            double totalDist = distances.Sum();
            double r = random.NextDouble() * totalDist;
            double cumulative = 0;
            for (int i = 0; i < distances.Length; i++)
            {
                cumulative += distances[i];
                if (cumulative >= r)
                {
                    centroids.Add(new Centroid { Color = samples[i] });
                    break;
                }
            }
        }

        return centroids;
    }

    private int FindNearestCentroid(SKColor color, List<Centroid> centroids)
    {
        int nearest = 0;
        double minDist = double.MaxValue;
        for (int i = 0; i < centroids.Count; i++)
        {
            double d = ColorDistance(color, centroids[i].Color);
            if (d < minDist)
            {
                minDist = d;
                nearest = i;
            }
        }
        return nearest;
    }

    private static double ColorDistance(SKColor a, SKColor b)
    {
        // Weighted Euclidean distance (perceptual)
        int dr = a.Red - b.Red;
        int dg = a.Green - b.Green;
        int db = a.Blue - b.Blue;
        return dr * dr * 0.299 + dg * dg * 0.587 + db * db * 0.114;
    }

    private void ApplyFloydSteinberg(SKColor[,] original, int[,] pixelGrid, List<SKColor> palette, int height, int width)
    {
        // Floyd-Steinberg dithering: diffuse quantization error to neighbors
        var error = new (double r, double g, double b)[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var orig = original[y, x];
                int idx = pixelGrid[y, x];
                var quantized = palette[idx];

                double er = orig.Red - quantized.Red + error[y, x].r;
                double eg = orig.Green - quantized.Green + error[y, x].g;
                double eb = orig.Blue - quantized.Blue + error[y, x].b;

                // Diffuse error
                if (x + 1 < width)
                {
                    error[y, x + 1].r += er * 7.0 / 16;
                    error[y, x + 1].g += eg * 7.0 / 16;
                    error[y, x + 1].b += eb * 7.0 / 16;
                }
                if (y + 1 < height)
                {
                    if (x > 0)
                    {
                        error[y + 1, x - 1].r += er * 3.0 / 16;
                        error[y + 1, x - 1].g += eg * 3.0 / 16;
                        error[y + 1, x - 1].b += eb * 3.0 / 16;
                    }
                    error[y + 1, x].r += er * 5.0 / 16;
                    error[y + 1, x].g += eg * 5.0 / 16;
                    error[y + 1, x].b += eb * 5.0 / 16;
                    if (x + 1 < width)
                    {
                        error[y + 1, x + 1].r += er * 1.0 / 16;
                        error[y + 1, x + 1].g += eg * 1.0 / 16;
                        error[y + 1, x + 1].b += eb * 1.0 / 16;
                    }
                }
            }
        }
    }

    private class Centroid
    {
        public SKColor Color { get; set; }
    }
}
