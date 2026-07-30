using System.Text.Json.Serialization;

namespace backend.DTOs;

public sealed class OsrmRouteResponse
{
    [JsonPropertyName("code")] public string Code { get; set; } = "";
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("routes")] public List<OsrmRoute> Routes { get; set; } = [];
}

public sealed class OsrmRoute
{
    [JsonPropertyName("geometry")] public OsrmLineString Geometry { get; set; } = new();
    [JsonPropertyName("distance")] public double Distance { get; set; }
    [JsonPropertyName("duration")] public double Duration { get; set; }
}

public sealed class OsrmLineString
{
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("coordinates")] public List<double[]> Coordinates { get; set; } = [];
}
