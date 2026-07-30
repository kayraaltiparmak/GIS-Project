using backend.DTOs;
using NetTopologySuite.Geometries;

namespace backend.Services;

public interface IOsrmService
{
    Task<OsrmRoute> GetRouteAsync(IReadOnlyList<Point> stops, CancellationToken cancellationToken);
}
