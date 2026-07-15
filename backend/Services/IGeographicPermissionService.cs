namespace backend.Services;

public interface IGeographicPermissionService
{
    Task<GeographicPermissionResult> CanCreateGeometryAsync(int userId, string wkt);
}
