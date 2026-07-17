using NetTopologySuite.Geometries;

namespace backend.Models;

public class ProvinceBoundary
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public Geometry Geometry { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}
