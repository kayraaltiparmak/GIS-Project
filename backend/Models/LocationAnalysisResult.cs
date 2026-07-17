using NetTopologySuite.Geometries;

namespace backend.Models;

public class LocationAnalysisResult
{
    public long Id { get; set; }
    public Guid AnalysisId { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int PoiId { get; set; }
    public TblPoi Poi { get; set; } = null!;
    public int CategoryId { get; set; }
    public TblPoiCategory Category { get; set; } = null!;
    public int CriterionWeight { get; set; }
    public double HeatWeight { get; set; }
    public Point Geometry { get; set; } = null!;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
