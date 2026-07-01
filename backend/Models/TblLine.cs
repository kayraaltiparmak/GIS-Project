using NetTopologySuite.Geometries;

namespace backend.Models;

public class TblLine
{
    public int Id { get; set; }

    public LineString Geometry { get; set; } = null!;
}
