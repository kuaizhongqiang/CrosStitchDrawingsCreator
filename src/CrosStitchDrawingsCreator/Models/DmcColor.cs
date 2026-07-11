using System.Text.Json.Serialization;

namespace CrosStitchDrawingsCreator.Models;

/// <summary>
/// DMC thread color entry from the built-in library.
/// </summary>
public class DmcColor
{
    [JsonPropertyName("dmc_code")]
    public string DmcCode { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("rgb")]
    public DmcRgb Rgb { get; set; } = new();

    [JsonPropertyName("hex")]
    public string Hex { get; set; } = string.Empty;
}

public class DmcRgb
{
    [JsonPropertyName("r")]
    public byte R { get; set; }

    [JsonPropertyName("g")]
    public byte G { get; set; }

    [JsonPropertyName("b")]
    public byte B { get; set; }
}

/// <summary>
/// Container for the DMC color library JSON.
/// </summary>
public class DmcColorLibrary
{
    [JsonPropertyName("colors")]
    public List<DmcColor> Colors { get; set; } = [];
}
