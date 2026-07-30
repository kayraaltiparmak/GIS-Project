using NetTopologySuite.Geometries;

namespace backend.Models;

public class TblTransportRoute
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#000000";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public int CreatedUserId { get; set; }
    public User CreatedUser { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public LineString? Geometry { get; set; }
    public double? DistanceMeters { get; set; }
    public double? DurationSeconds { get; set; }
    public DateTime? RouteCalculatedAt { get; set; }
    public string? RouteCalculationStatus { get; set; }
    public string? RouteCalculationError { get; set; }
    public ICollection<TblTransportStop> Stops { get; set; } = [];
}
