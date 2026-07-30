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
    public ICollection<TblTransportStop> Stops { get; set; } = [];
}
