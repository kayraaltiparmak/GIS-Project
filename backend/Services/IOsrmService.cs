using backend.DTOs;
using NetTopologySuite.Geometries;

namespace backend.Services;

public interface IOsrmService
{
    Task<OsrmRoute> GetRouteAsync(IReadOnlyList<Point> stops, CancellationToken cancellationToken);
    Task<OsrmRoute> GetWalkingRouteAsync(Point start, Point destination, string baseUrl, CancellationToken cancellationToken);
}
