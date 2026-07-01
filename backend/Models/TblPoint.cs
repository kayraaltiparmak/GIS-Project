using NetTopologySuite.Geometries;

namespace backend.Models;

public class TblPoint
{
    public int Id { get; set; }

    public Point Geometry { get; set; } = null!;
}
