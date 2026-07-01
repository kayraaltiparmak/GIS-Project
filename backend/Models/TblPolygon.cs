using NetTopologySuite.Geometries;

namespace backend.Models;

public class TblPolygon
{
    public int Id { get; set; }

    public Polygon Geometry { get; set; } = null!;
}
