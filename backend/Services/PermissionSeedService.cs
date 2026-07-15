using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public static class PermissionSeedService
{
    private static readonly PermissionSeedItem[] RequiredPermissions =
    [
        new("Heatmap.Analyze", "Isı Haritası Analizi", "GeoServer üzerinden ısı haritası analizi yapma yetkisi")
    ];

    public static async Task EnsureRequiredPermissionsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var changed = false;

        foreach (var requiredPermission in RequiredPermissions)
        {
            var permission = await context.TblPermissions
                .FirstOrDefaultAsync(item => item.Code == requiredPermission.Code);

            if (permission == null)
            {
                permission = new TblPermission
                {
                    Code = requiredPermission.Code,
                    Name = requiredPermission.Name,
                    Description = requiredPermission.Description,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedDate = now,
                    ModifiedDate = now
                };

                context.TblPermissions.Add(permission);
                changed = true;
                continue;
            }

            if (permission.Name != requiredPermission.Name ||
                permission.Description != requiredPermission.Description ||
                !permission.IsActive ||
                permission.IsDeleted)
            {
                permission.Name = requiredPermission.Name;
                permission.Description = requiredPermission.Description;
                permission.IsActive = true;
                permission.IsDeleted = false;
                permission.ModifiedDate = now;
                changed = true;
            }
        }

        if (changed)
        {
            await context.SaveChangesAsync();
        }

        await EnsureAdminRoleHasAllPermissionsAsync(context, now);
    }

    private static async Task EnsureAdminRoleHasAllPermissionsAsync(AppDbContext context, DateTime now)
    {
        var adminRole = await context.TblRoles
            .FirstOrDefaultAsync(role => role.Name.ToLower() == "admin" && role.IsActive && !role.IsDeleted);

        if (adminRole == null)
        {
            return;
        }

        var permissions = await context.TblPermissions
            .Where(item => item.IsActive && !item.IsDeleted)
            .ToListAsync();

        if (permissions.Count == 0)
        {
            return;
        }

        var existingRolePermissions = await context.TblRolePermissions
            .Where(item => item.RoleId == adminRole.Id)
            .ToListAsync();
        var changed = false;

        foreach (var permission in permissions)
        {
            var rolePermission = existingRolePermissions
                .FirstOrDefault(item => item.PermissionId == permission.Id);

            if (rolePermission == null)
            {
                context.TblRolePermissions.Add(new TblRolePermission
                {
                    RoleId = adminRole.Id,
                    PermissionId = permission.Id,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedDate = now,
                    ModifiedDate = now
                });
                changed = true;
                continue;
            }

            if (!rolePermission.IsActive || rolePermission.IsDeleted)
            {
                rolePermission.IsActive = true;
                rolePermission.IsDeleted = false;
                rolePermission.ModifiedDate = now;
                changed = true;
            }
        }

        if (changed)
        {
            await context.SaveChangesAsync();
        }
    }

    private sealed record PermissionSeedItem(string Code, string Name, string Description);
}
