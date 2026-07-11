namespace CrosStitchDrawingsCreator.Services;

/// <summary>
/// Calculates fabric dimensions based on stitch count and fabric count (CT).
/// </summary>
public class FabricCalculator
{
    /// <summary>
    /// Calculate fabric dimensions for cross-stitch.
    /// </summary>
    /// <param name="stitchWidth">Pattern width in stitches.</param>
    /// <param name="stitchHeight">Pattern height in stitches.</param>
    /// <param name="fabricCount">Fabric count (CT), e.g. 14, 16, 18.</param>
    /// <param name="borderInches">Additional border around the design in inches.</param>
    /// <returns>Fabric dimensions result.</returns>
    public FabricResult Calculate(int stitchWidth, int stitchHeight, int fabricCount, double borderInches = 3.0)
    {
        double designWidthInches = (double)stitchWidth / fabricCount;
        double designHeightInches = (double)stitchHeight / fabricCount;

        double fabricWidthInches = designWidthInches + borderInches;
        double fabricHeightInches = designHeightInches + borderInches;

        // Convert to cm
        double fabricWidthCm = fabricWidthInches * 2.54;
        double fabricHeightCm = fabricHeightInches * 2.54;

        return new FabricResult
        {
            StitchWidth = stitchWidth,
            StitchHeight = stitchHeight,
            FabricCount = fabricCount,
            DesignWidthInches = designWidthInches,
            DesignHeightInches = designHeightInches,
            FabricWidthInches = fabricWidthInches,
            FabricHeightInches = fabricHeightInches,
            FabricWidthCm = fabricWidthCm,
            FabricHeightCm = fabricHeightCm,
            BorderInches = borderInches
        };
    }
}

public class FabricResult
{
    public int StitchWidth { get; set; }
    public int StitchHeight { get; set; }
    public int FabricCount { get; set; }
    public double DesignWidthInches { get; set; }
    public double DesignHeightInches { get; set; }
    public double FabricWidthInches { get; set; }
    public double FabricHeightInches { get; set; }
    public double FabricWidthCm { get; set; }
    public double FabricHeightCm { get; set; }
    public double BorderInches { get; set; }

    public string Summary =>
        $"设计: {DesignWidthInches:F1}\"×{DesignHeightInches:F1}\" ({DesignWidthInches * 2.54:F1}×{DesignHeightInches * 2.54:F1} cm)\n" +
        $"布料 ({FabricCount}CT): {FabricWidthInches:F1}\"×{FabricHeightInches:F1}\" ({FabricWidthCm:F1}×{FabricHeightCm:F1} cm)";
}
