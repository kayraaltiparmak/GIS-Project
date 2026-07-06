using NetTopologySuite.Geometries;

namespace backend.Models;

public class TblPolygon
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public int? ModifiedUserId { get; set; }

    public User? ModifiedUser { get; set; }

    public string Name { get; set; } = "";

    public string Color { get; set; } = "";

    public Polygon Geometry { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}
