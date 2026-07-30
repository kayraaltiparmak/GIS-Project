using System.ComponentModel.DataAnnotations;
namespace backend.DTOs;
public sealed class TransportRouteRequest
{
    [Required, MinLength(1)] public string Name { get; set; } = "";
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color #RRGGBB HEX formatinda olmalidir.")] public string Color { get; set; } = "#000000";
    public bool IsActive { get; set; } = true;
}
public class TransportRouteResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Color { get; set; } = "";
    public int StopCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsActive { get; set; }
    public object? Geometry { get; set; }
    public double? DistanceMeters { get; set; }
    public double? DurationSeconds { get; set; }
    public DateTime? RouteCalculatedAt { get; set; }
    public string? RouteCalculationStatus { get; set; }
}
public sealed class TransportRouteDetailResponse : TransportRouteResponse { public List<TransportStopResponse> Stops { get; set; } = []; }
public sealed class CalculateTransportRouteResponse
{
    public int RouteId { get; set; }
    public object Geometry { get; set; } = null!;
    public double DistanceMeters { get; set; }
    public double DurationSeconds { get; set; }
    public DateTime CalculatedAt { get; set; }
}
public sealed class TransportStopRequest
{
    [Required, MinLength(1)] public string Name { get; set; } = "";
    public string Wkt { get; set; } = "";
    public TransportPointRequest? Geometry { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "RouteId zorunludur.")] public int RouteId { get; set; }
    [Range(0, int.MaxValue, ErrorMessage = "SequenceNumber negatif olamaz.")] public int SequenceNumber { get; set; }
    public bool IsActive { get; set; } = true;
}
public sealed class TransportPointRequest { public string Type { get; set; } = "Point"; public double[] Coordinates { get; set; } = []; }
public sealed class TransportStopResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Geometry { get; set; } = "";
    public int RouteId { get; set; }
    public string RouteName { get; set; } = "";
    public int SequenceNumber { get; set; }
    public bool IsActive { get; set; }
    public string RouteColor { get; set; } = "";
    public int CreatedUserId { get; set; }
    public string CreatedUsername { get; set; } = "";
    public DateTime CreatedDate { get; set; }
}
public sealed class ReorderTransportStopsRequest { public List<int> StopIds { get; set; } = []; }
