using backend.DTOs;

namespace backend.Services;

public interface IVehicleSimulationService
{
    Task<VehicleSimulationStartResult> StartSimulationAsync(int routeId, int startedByUserId, CancellationToken cancellationToken);
    Task<VehicleSimulationStopResult> StopSimulationAsync(int routeId, CancellationToken cancellationToken);
    VehicleSimulationStateDto? GetSimulationState(int routeId);
    bool IsSimulationRunning(int routeId);
}

public sealed record VehicleSimulationStartResult(bool Success, bool Conflict, string? Error, VehicleSimulationStateDto? State);
public sealed record VehicleSimulationStopResult(bool Success, string? Error, VehicleSimulationStateDto? State);
