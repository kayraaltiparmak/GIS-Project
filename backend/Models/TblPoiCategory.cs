namespace backend.Models;

public class TblPoiCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string IconKey { get; set; } = "default";
    public int? ParentId { get; set; }
    public TblPoiCategory? Parent { get; set; }
    public ICollection<TblPoiCategory> Children { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public int? CreatedUserId { get; set; }
    public User? CreatedUser { get; set; }
    public int? ModifiedUserId { get; set; }
    public User? ModifiedUser { get; set; }
}
