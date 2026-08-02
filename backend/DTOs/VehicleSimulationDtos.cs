namespace backend.DTOs;

public sealed record VehiclePositionDto(
    int RouteId,
    string RouteName,
    double Longitude,
    double Latitude,
    double ProgressPercent,
    string Status,
    DateTime StartedAt,
    DateTime UpdatedAt);

public sealed record VehicleSimulationStateDto(
    int RouteId,
    string RouteName,
    double Longitude,
    double Latitude,
    double ProgressPercent,
    string Status,
    DateTime StartedAt,
    DateTime UpdatedAt,
    int StartedByUserId,
    int CurrentSegmentIndex,
    bool IsRunning,
    bool IsCompleted);

public sealed record VehicleSimulationStatusResponse(
    int RouteId,
    bool IsRunning,
    bool CanFollow,
    string? Status,
    VehicleSimulationStateDto? Simulation);

public sealed record VehicleSimulationActionResponse(
    string Message,
    VehicleSimulationStateDto Simulation);
