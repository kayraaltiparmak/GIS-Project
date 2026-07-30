using backend.DTOs;

namespace backend.Services;

public interface ITransportRouteCalculationService
{
    Task<RouteCalculationResult> CalculateAsync(int routeId, CancellationToken cancellationToken);
}

public sealed record RouteCalculationResult(bool Success, string? Error, CalculateTransportRouteResponse? Response);
