namespace backend.DTOs;

public class AuthMeResponse
{
    public int UserId { get; set; }

    public string Username { get; set; } = "";

    public int? RoleId { get; set; }

    public string? RoleName { get; set; }

    public List<string> Permissions { get; set; } = [];

    public bool IsAdmin { get; set; }
}
