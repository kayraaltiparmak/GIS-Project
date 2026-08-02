using System.Security.Claims;
using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[Authorize, ApiController, Route("api/transport/routes/{routeId:int}/simulation")]
public sealed class VehicleSimulationsController : ControllerBase
{
    private readonly IVehicleSimulationService _simulations;
    private readonly ITransportRouteAccessService _routeAccess;
    private readonly IUserPermissionService _permissions;
    private readonly ILogger<VehicleSimulationsController> _logger;

    public VehicleSimulationsController(
        IVehicleSimulationService simulations,
        ITransportRouteAccessService routeAccess,
        IUserPermissionService permissions,
        ILogger<VehicleSimulationsController> logger) =>
        (_simulations, _routeAccess, _permissions, _logger) = (simulations, routeAccess, permissions, logger);

    [HttpPost("start")]
    public async Task<IActionResult> Start(int routeId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = UserId(); if (userId == null) return Unauthorized();
            if (!await Allowed(userId.Value, TransportPermissionCodes.SimulationStart)) return Forbid();
            if (!await _routeAccess.CanViewAsync(userId.Value, routeId, cancellationToken)) return NotFound("Guzergah bulunamadi veya goruntuleme yetkiniz yok.");
            var result = await _simulations.StartSimulationAsync(routeId, userId.Value, cancellationToken);
            if (result.Conflict) return Conflict(result.Error);
            if (!result.Success || result.State == null) return BadRequest(result.Error);
            return Accepted(new VehicleSimulationActionResponse("Simulasyon baslatildi.", result.State));
        }
        catch (OperationCanceledException) { return StatusCode(499); }
        catch (Exception ex) { return Error(ex); }
    }

    [HttpPost("stop")]
    public async Task<IActionResult> Stop(int routeId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = UserId(); if (userId == null) return Unauthorized();
            if (!await Allowed(userId.Value, TransportPermissionCodes.SimulationStop)) return Forbid();
            if (!await _routeAccess.CanViewAsync(userId.Value, routeId, cancellationToken)) return NotFound("Guzergah bulunamadi veya goruntuleme yetkiniz yok.");
            var result = await _simulations.StopSimulationAsync(routeId, cancellationToken);
            if (!result.Success || result.State == null) return NotFound(result.Error);
            return Ok(new VehicleSimulationActionResponse("Simulasyon durdurma istegi alindi.", result.State));
        }
        catch (OperationCanceledException) { return StatusCode(499); }
        catch (Exception ex) { return Error(ex); }
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status(int routeId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = UserId(); if (userId == null) return Unauthorized();
            if (!await _routeAccess.CanViewAsync(userId.Value, routeId, cancellationToken)) return Forbid();
            var state = _simulations.GetSimulationState(routeId);
            return Ok(new VehicleSimulationStatusResponse(routeId, state?.IsRunning == true,
                state?.IsRunning == true, state?.Status, state));
        }
        catch (OperationCanceledException) { return StatusCode(499); }
        catch (Exception ex) { return Error(ex); }
    }

    private int? UserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private Task<bool> Allowed(int userId, string code) => _permissions.HasPermissionAsync(userId, code);
    private IActionResult Error(Exception ex)
    {
        _logger.LogError(ex, "Vehicle simulation endpoint failed.");
        return StatusCode(500, "Islem sirasinda beklenmeyen bir hata olustu.");
    }
}
