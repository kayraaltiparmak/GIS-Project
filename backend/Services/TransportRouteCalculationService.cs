using backend.Data;
using backend.DTOs;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace backend.Services;

public sealed class TransportRouteCalculationService : ITransportRouteCalculationService
{
    private readonly AppDbContext _db;
    private readonly IOsrmService _osrm;
    private readonly ILogger<TransportRouteCalculationService> _logger;

    public TransportRouteCalculationService(AppDbContext db, IOsrmService osrm, ILogger<TransportRouteCalculationService> logger) =>
        (_db, _osrm, _logger) = (db, osrm, logger);

    public async Task<RouteCalculationResult> CalculateAsync(int routeId, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var route = await _db.TblTransportRoutes.FirstOrDefaultAsync(x => x.Id == routeId && x.IsActive && !x.IsDeleted, cancellationToken);
        if (route == null) return new(false, "Guzergah bulunamadi.", null);
        try
        {
            var stops = await _db.TblTransportStops.Where(x => x.RouteId == routeId && x.IsActive && !x.IsDeleted)
                .OrderBy(x => x.SequenceNumber).ThenBy(x => x.Id).Select(x => x.Geometry).AsNoTracking().ToListAsync(cancellationToken);
            if (stops.Count < 2) throw new InvalidOperationException("Rota hesaplamak icin en az iki aktif durak gereklidir.");
            if (stops.Any(point => point == null || point.IsEmpty || point.X is < -180 or > 180 || point.Y is < -90 or > 90))
                throw new InvalidOperationException("Duraklardan en az birinin koordinati gecersizdir.");
            var osrmRoute = await _osrm.GetRouteAsync(stops, cancellationToken);
            var coordinates = osrmRoute.Geometry.Coordinates.Select(item =>
            {
                if (item.Length < 2 || item[0] is < -180 or > 180 || item[1] is < -90 or > 90)
                    throw new InvalidOperationException("Rota servisi gecersiz koordinat dondurdu.");
                return new Coordinate(item[0], item[1]);
            }).ToArray();
            var geometry = new LineString(coordinates) { SRID = 4326 };
            var calculatedAt = DateTime.UtcNow;
            route.Geometry = geometry;
            route.DistanceMeters = osrmRoute.Distance;
            route.DurationSeconds = osrmRoute.Duration;
            route.RouteCalculatedAt = calculatedAt;
            route.RouteCalculationStatus = "Success";
            route.RouteCalculationError = null;
            route.ModifiedDate = calculatedAt;
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(true, null, new CalculateTransportRouteResponse
            {
                RouteId = routeId,
                Geometry = new { type = "LineString", coordinates = osrmRoute.Geometry.Coordinates },
                DistanceMeters = osrmRoute.Distance,
                DurationSeconds = osrmRoute.Duration,
                CalculatedAt = calculatedAt
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route calculation failed for route {RouteId}.", routeId);
            route.RouteCalculationStatus = "Failed";
            route.RouteCalculationError = ex is InvalidOperationException ? ex.Message : "Rota hesaplanamadi. Lutfen daha sonra tekrar deneyin.";
            route.ModifiedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(false, route.RouteCalculationError, null);
        }
    }
}
