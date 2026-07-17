using NetTopologySuite.Geometries;
namespace backend.Models;
public class TblPoi
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int CategoryId { get; set; }
    public TblPoiCategory Category { get; set; } = null!;
    public string WorkingHours { get; set; } = "";
    public string IconKey { get; set; } = "default";
    public Point Geometry { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public int? ModifiedUserId { get; set; }
    public User? ModifiedUser { get; set; }
}
