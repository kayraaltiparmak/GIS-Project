using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public sealed class TransportRouteAccessService : ITransportRouteAccessService
{
    private readonly AppDbContext _db;
    private readonly IUserPermissionService _permissions;

    public TransportRouteAccessService(AppDbContext db, IUserPermissionService permissions) =>
        (_db, _permissions) = (db, permissions);

    public async Task<bool> CanViewAsync(int userId, int routeId, CancellationToken cancellationToken)
    {
        if (routeId <= 0) return false;
        var permitted = await _permissions.HasPermissionAsync(userId, TransportPermissionCodes.RouteView)
            || await _permissions.HasPermissionAsync(userId, TransportPermissionCodes.SimulationView);
        return permitted && await _db.TblTransportRoutes.AsNoTracking()
            .AnyAsync(x => x.Id == routeId && x.IsActive && !x.IsDeleted, cancellationToken);
    }
}
