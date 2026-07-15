using backend.Data;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace backend.Services;

public class GeographicPermissionService : IGeographicPermissionService
{
    private readonly AppDbContext _context;

    public GeographicPermissionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<GeographicPermissionResult> CanCreateGeometryAsync(int userId, string wkt)
    {
        var geometry = ReadGeometry(wkt);

        if (geometry == null)
        {
            return GeographicPermissionResult.Denied("Gecersiz geometri.");
        }

        var userArea = await _context.TblUserGeographicPermissions
            .Where(item => item.UserId == userId && item.IsActive && !item.IsDeleted)
            .OrderByDescending(item => item.ModifiedDate)
            .Select(item => item.Geometry)
            .FirstOrDefaultAsync();

        if (userArea != null)
        {
            return userArea.Covers(geometry)
                ? GeographicPermissionResult.Allowed()
                : GeographicPermissionResult.Denied("Cizim kullaniciya tanimli cografi yetki alaninin disinda.");
        }

        var roleArea = await _context.Users
            .Where(user => user.Id == userId && user.RoleId != null && user.IsActive && !user.IsDeleted)
            .Join(
                _context.TblRoleGeographicPermissions.Where(item => item.IsActive && !item.IsDeleted),
                user => user.RoleId!.Value,
                permission => permission.RoleId,
                (user, permission) => permission)
            .OrderByDescending(item => item.ModifiedDate)
            .Select(item => item.Geometry)
            .FirstOrDefaultAsync();

        if (roleArea == null)
        {
            return GeographicPermissionResult.Denied("Kullanıcı için aktif coğrafi yetki alanı bulunamadı.");
        }

        return roleArea.Covers(geometry)
            ? GeographicPermissionResult.Allowed()
            : GeographicPermissionResult.Denied("Cizim role tanimli cografi yetki alaninin disinda.");
    }

    private static Geometry? ReadGeometry(string wkt)
    {
        if (string.IsNullOrWhiteSpace(wkt))
        {
            return null;
        }

        try
        {
            var geometry = new WKTReader().Read(wkt);
            geometry.SRID = 4326;

            return geometry;
        }
        catch
        {
            return null;
        }
    }
}
