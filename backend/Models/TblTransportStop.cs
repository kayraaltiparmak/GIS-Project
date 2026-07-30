using NetTopologySuite.Geometries;
namespace backend.Models;
public class TblTransportStop
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public Point Geometry { get; set; } = null!;
    public int RouteId { get; set; }
    public TblTransportRoute Route { get; set; } = null!;
    public int SequenceNumber { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public int CreatedUserId { get; set; }
    public User CreatedUser { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
}
