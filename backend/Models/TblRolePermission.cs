namespace backend.Models;

public class TblRolePermission
{
    public int Id { get; set; }

    public int RoleId { get; set; }

    public TblRole Role { get; set; } = null!;

    public int PermissionId { get; set; }

    public TblPermission Permission { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}
