using System.Security.Claims;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace backend.Hubs;

[Authorize]
public sealed class VehicleSimulationHub : Hub
{
    private readonly ITransportRouteAccessService _routeAccess;

    public VehicleSimulationHub(ITransportRouteAccessService routeAccess) => _routeAccess = routeAccess;

    public async Task JoinRouteGroup(int routeId)
    {
        var userId = UserId();
        if (routeId <= 0) throw new HubException("Gecerli bir guzergah kimligi zorunludur.");
        if (userId == null || !await _routeAccess.CanViewAsync(userId.Value, routeId, Context.ConnectionAborted))
            throw new HubException("Bu guzergahi takip etme yetkiniz yok veya guzergah aktif degil.");
        await Groups.AddToGroupAsync(Context.ConnectionId, VehicleSimulationGroups.ForRoute(routeId), Context.ConnectionAborted);
    }

    public async Task LeaveRouteGroup(int routeId)
    {
        if (routeId <= 0) throw new HubException("Gecerli bir guzergah kimligi zorunludur.");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, VehicleSimulationGroups.ForRoute(routeId), Context.ConnectionAborted);
    }

    private int? UserId() => int.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
