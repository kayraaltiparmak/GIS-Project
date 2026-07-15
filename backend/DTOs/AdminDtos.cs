namespace backend.DTOs;

public class AdminUserRequest
{
    public string Username { get; set; } = "";

    public string? Password { get; set; }

    public int? RoleId { get; set; }

    public bool IsActive { get; set; } = true;
}

public class AdminRoleRequest
{
    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public bool IsActive { get; set; } = true;
}

public class AdminPermissionRequest
{
    public string Name { get; set; } = "";

    public string Code { get; set; } = "";

    public string Description { get; set; } = "";

    public bool IsActive { get; set; } = true;
}

public class PermissionAssignmentRequest
{
    public List<int> PermissionIds { get; set; } = [];
}

public class GeographicPermissionRequest
{
    public string Wkt { get; set; } = "";
}
