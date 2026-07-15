namespace backend.Models;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = "";

    public string Password { get; set; } = "";

    public int? RoleId { get; set; }

    public TblRole? Role { get; set; }

    public bool IsDeleted { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public DateTime ModifiedDate { get; set; } = DateTime.Now;
}
