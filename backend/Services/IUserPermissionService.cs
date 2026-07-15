using backend.DTOs;

namespace backend.Services;

public interface IUserPermissionService
{
    Task<AuthMeResponse?> GetCurrentUserAsync(int userId);

    Task<bool> HasPermissionAsync(int userId, string permissionCode);
}
