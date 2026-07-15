using backend.Data;
using backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class UserPermissionService : IUserPermissionService
{
    private static readonly string[] AdminPermissionCodes =
    [
        "User.Manage",
        "Role.Manage",
        "Permission.Manage"
    ];

    private readonly AppDbContext _context;

    public UserPermissionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AuthMeResponse?> GetCurrentUserAsync(int userId)
    {
        var user = await _context.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive && !x.IsDeleted);

        if (user == null)
        {
            return null;
        }

        var permissions = await GetPermissionCodesAsync(userId);
        var roleName = user.Role?.IsActive == true && user.Role.IsDeleted == false ? user.Role.Name : null;

        return new AuthMeResponse
        {
            UserId = user.Id,
            Username = user.Username,
            RoleId = user.RoleId,
            RoleName = roleName,
            Permissions = permissions,
            IsAdmin = IsAdmin(roleName, permissions)
        };
    }

    public async Task<bool> HasPermissionAsync(int userId, string permissionCode)
    {
        var permissions = await GetPermissionCodesAsync(userId);

        return permissions.Contains(permissionCode);
    }

    private async Task<List<string>> GetPermissionCodesAsync(int userId)
    {
        var isAdminRole = await _context.Users
            .Where(user => user.Id == userId && user.IsActive && !user.IsDeleted)
            .Join(
                _context.TblRoles.Where(role => role.IsActive && !role.IsDeleted),
                user => user.RoleId,
                role => role.Id,
                (user, role) => role.Name)
            .AnyAsync(roleName => roleName.ToLower() == "admin");

        var rolePermissions = await _context.Users
            .Where(user => user.Id == userId && user.IsActive && !user.IsDeleted)
            .Where(user => user.RoleId != null)
            .Join(
                _context.TblRoles.Where(role => role.IsActive && !role.IsDeleted),
                user => user.RoleId!.Value,
                role => role.Id,
                (user, role) => role.Id)
            .Join(
                _context.TblRolePermissions.Where(rolePermission => rolePermission.IsActive && !rolePermission.IsDeleted),
                roleId => roleId,
                rolePermission => rolePermission.RoleId,
                (roleId, rolePermission) => rolePermission.PermissionId)
            .Join(
                _context.TblPermissions.Where(permission => permission.IsActive && !permission.IsDeleted),
                permissionId => permissionId,
                permission => permission.Id,
                (permissionId, permission) => permission.Code)
            .ToListAsync();

        var userPermissions = await _context.TblUserPermissions
            .Where(userPermission => userPermission.UserId == userId && userPermission.IsActive && !userPermission.IsDeleted)
            .Join(
                _context.TblPermissions.Where(permission => permission.IsActive && !permission.IsDeleted),
                userPermission => userPermission.PermissionId,
                permission => permission.Id,
                (userPermission, permission) => permission.Code)
            .ToListAsync();

        var adminPermissions = isAdminRole
            ? await _context.TblPermissions
                .Where(permission => permission.IsActive && !permission.IsDeleted)
                .Select(permission => permission.Code)
                .ToListAsync()
            : [];

        return rolePermissions
            .Concat(userPermissions)
            .Concat(adminPermissions)
            .Distinct()
            .OrderBy(code => code)
            .ToList();
    }

    private static bool IsAdmin(string? roleName, IReadOnlyCollection<string> permissions)
    {
        return string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase) ||
               AdminPermissionCodes.Any(permissions.Contains);
    }
}
