using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public static class PermissionSeedService
{
    private static readonly PermissionSeedItem[] RequiredPermissions =
    [
        new("Heatmap.Analyze", "Isı Haritası Analizi", "GeoServer üzerinden ısı haritası analizi yapma yetkisi"),
        new("Poi.View", "POI görüntüleme", "POI kayıtlarını görüntüleme yetkisi"),
        new("Poi.Create", "POI ekleme", "POI kaydı ekleme yetkisi"),
        new("Poi.Update", "POI güncelleme", "POI kaydı güncelleme yetkisi"),
        new("Poi.Delete", "POI silme", "POI kaydını pasife alma yetkisi"),
        new("Poi.Manage", "POI yönetimi", "Tüm POI kayıtlarını yönetme yetkisi"),
        new("PoiCategory.Manage", "POI kategori yönetimi", "POI kategorilerini yönetme yetkisi"),
        new("Poi.Search", "POI arama", "POI kayıtlarında arama yetkisi")
        ,new("LocationAnalysis.Use", "Konum Analizi", "Konum analizi özelliğini kullanma yetkisi"),
        new("TransportRoute.View", "Güzergah görüntüleme", "Ulaşım güzergahlarını görüntüleme yetkisi"),
        new("TransportRoute.Create", "Güzergah ekleme", "Ulaşım güzergahı ekleme yetkisi"),
        new("TransportRoute.Update", "Güzergah güncelleme", "Ulaşım güzergahı güncelleme yetkisi"),
        new("TransportRoute.Calculate", "Güzergah hesaplama", "Ulaşım güzergahını OSRM ile hesaplama yetkisi"),
        new("TransportRoute.Delete", "Güzergah silme", "Ulaşım güzergahını pasife alma yetkisi"),
        new("TransportStop.View", "Durak görüntüleme", "Ulaşım duraklarını görüntüleme yetkisi"),
        new("TransportStop.Create", "Durak ekleme", "Ulaşım durağı ekleme yetkisi"),
        new("TransportStop.Update", "Durak güncelleme", "Ulaşım durağı güncelleme yetkisi"),
        new("TransportStop.Delete", "Durak silme", "Ulaşım durağını pasife alma yetkisi")
    ];

    public static async Task EnsureRequiredPermissionsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        foreach (var item in RequiredPermissions)
        {
            var permission = await context.TblPermissions.FirstOrDefaultAsync(x => x.Code == item.Code);
            if (permission == null)
                context.TblPermissions.Add(new TblPermission { Code = item.Code, Name = item.Name, Description = item.Description, CreatedDate = now, ModifiedDate = now });
            else { permission.Name = item.Name; permission.Description = item.Description; permission.IsActive = true; permission.IsDeleted = false; permission.ModifiedDate = now; }
        }
        await context.SaveChangesAsync();
        await EnsureRoleAsync(context, "Admin", null, now);
        await EnsureRoleAsync(context, "Operator", ["Poi.View", "Poi.Create", "Poi.Update", "Poi.Search", "LocationAnalysis.Use"], now);
        await EnsureRoleAsync(context, "User", ["Poi.View", "Poi.Search", "LocationAnalysis.Use"], now);
        await EnsureRoleAsync(context, "Transport Operator", ["TransportRoute.View", "TransportRoute.Create", "TransportRoute.Update", "TransportRoute.Delete", "TransportRoute.Calculate", "TransportStop.View", "TransportStop.Create", "TransportStop.Update", "TransportStop.Delete"], now);
        await EnsureRoleAsync(context, "Transport User", ["TransportRoute.View", "TransportStop.View"], now);
    }

    private static async Task EnsureRoleAsync(AppDbContext context, string name, string[]? codes, DateTime now)
    {
        var role = await context.TblRoles.FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower());
        if (role == null)
        {
            role = new TblRole { Name = name, Description = name + " rolü", CreatedDate = now, ModifiedDate = now };
            context.TblRoles.Add(role);
            await context.SaveChangesAsync();
        }
        else { role.IsActive = true; role.IsDeleted = false; role.ModifiedDate = now; }
        var ids = await context.TblPermissions.Where(x => (codes == null || codes.Contains(x.Code)) && x.IsActive && !x.IsDeleted).Select(x => x.Id).ToListAsync();
        var links = await context.TblRolePermissions.Where(x => x.RoleId == role.Id && ids.Contains(x.PermissionId)).ToListAsync();
        foreach (var id in ids)
        {
            var link = links.FirstOrDefault(x => x.PermissionId == id);
            if (link == null) context.TblRolePermissions.Add(new TblRolePermission { RoleId = role.Id, PermissionId = id, CreatedDate = now, ModifiedDate = now });
            else { link.IsActive = true; link.IsDeleted = false; link.ModifiedDate = now; }
        }
        await context.SaveChangesAsync();
    }

    private sealed record PermissionSeedItem(string Code, string Name, string Description);
}
