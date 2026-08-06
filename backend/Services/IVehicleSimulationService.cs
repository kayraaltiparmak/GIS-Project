using backend.DTOs;

namespace backend.Services;

public interface IVehicleSimulationService
{
    Task<VehicleSimulationStartResult> StartSimulationAsync(int routeId, int startedByUserId, double? speedKmh, CancellationToken cancellationToken);
    Task<VehicleSimulationStopResult> StopSimulationAsync(int routeId, CancellationToken cancellationToken);
    Task<VehicleSimulationSpeedResult> UpdateSpeedAsync(int routeId, double speedKmh, int changedByUserId, CancellationToken cancellationToken);
    VehicleSimulationStateDto? GetSimulationState(int routeId);
    bool IsSimulationRunning(int routeId);
}

public sealed record VehicleSimulationStartResult(bool Success, bool Conflict, string? Error, VehicleSimulationStateDto? State);
public sealed record VehicleSimulationStopResult(bool Success, string? Error, VehicleSimulationStateDto? State);
public sealed record VehicleSimulationSpeedResult(bool Success, string? Error, VehicleSimulationStateDto? State);
