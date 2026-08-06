using backend.Data;
using backend.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using NetTopologySuite.LinearReferencing;
using Npgsql;

namespace backend.Services;

public sealed class JourneyPlannerService : IJourneyPlannerService
{
    private const double MaximumStopRouteDistanceMeters = 250;
    private readonly AppDbContext _db;
    private readonly IOsrmService _osrm;
    private readonly JourneyPlannerOptions _options;
    private readonly ILogger<JourneyPlannerService> _logger;

    public JourneyPlannerService(AppDbContext db, IOsrmService osrm,
        IOptions<JourneyPlannerOptions> options, ILogger<JourneyPlannerService> logger) =>
        (_db, _osrm, _options, _logger) = (db, osrm, options.Value, logger);

    public async Task<JourneyPlanResponse> PlanAsync(JourneyPlanRequest request, CancellationToken cancellationToken)
    {
        var walkingLimit = request.MaximumWalkingDistanceMeters ?? _options.DefaultMaximumWalkingDistanceMeters;
        var starts = await FindCandidatesAsync(request.StartLongitude, request.StartLatitude, walkingLimit, cancellationToken);
        var destinations = await FindCandidatesAsync(request.DestinationLongitude, request.DestinationLatitude, walkingLimit, cancellationToken);

        var routeIds = starts.Select(x => x.RouteId).Intersect(destinations.Select(x => x.RouteId)).Distinct().ToArray();
        if (routeIds.Length == 0) return NotFound();
        var routes = await _db.TblTransportRoutes.AsNoTracking()
            .Where(x => routeIds.Contains(x.Id) && x.IsActive && !x.IsDeleted && x.Geometry != null)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        JourneyCandidate? best = null;
        foreach (var start in starts)
        foreach (var destination in destinations)
        {
            if (start.RouteId != destination.RouteId || start.SequenceNumber >= destination.SequenceNumber
                || !routes.TryGetValue(start.RouteId, out var route) || route.Geometry == null) continue;
            var transit = TryBuildTransit(route.Geometry, start, destination, route.DistanceMeters, route.DurationSeconds);
            if (transit == null) continue;
            var walkingSeconds = (start.WalkingDistanceMeters + destination.WalkingDistanceMeters) / _options.WalkingSpeedMetersPerSecond;
            var candidate = new JourneyCandidate(start, destination, route.Name, route.Color, transit.Value.Geometry,
                transit.Value.DistanceMeters, transit.Value.DurationSeconds, walkingSeconds);
            if (best == null || CandidateOrder(candidate, best) < 0) best = candidate;
        }
        if (best == null) return NotFound();

        var startPoint = Point(request.StartLongitude, request.StartLatitude);
        var boardingPoint = Point(best.Boarding.Longitude, best.Boarding.Latitude);
        var alightingPoint = Point(best.Alighting.Longitude, best.Alighting.Latitude);
        var destinationPoint = Point(request.DestinationLongitude, request.DestinationLatitude);
        var walkToStop = await BuildWalkingStepAsync(1, JourneyStepTypes.WalkToStop,
            $"{best.Boarding.StopName} durağına yürüyün", startPoint, boardingPoint, best.Boarding.WalkingDistanceMeters, cancellationToken);
        var walkToDestination = await BuildWalkingStepAsync(3, JourneyStepTypes.WalkToDestination,
            "Hedefe yürüyün", alightingPoint, destinationPoint, best.Alighting.WalkingDistanceMeters, cancellationToken);
        var stopCount = best.Alighting.SequenceNumber - best.Boarding.SequenceNumber;
        var transitStep = new JourneyStepDto(2, JourneyStepTypes.RideTransit,
            $"{best.RouteName} güzergahına binin",
            $"{best.Boarding.StopName} durağından binip {best.Alighting.StopName} durağında inin.",
            best.TransitDistanceMeters, best.TransitDurationSeconds, ToGeoJson(best.TransitGeometry), false,
            best.Boarding.RouteId, best.RouteName, best.RouteColor, best.Boarding.StopId, best.Alighting.StopId, stopCount);
        var steps = new[] { walkToStop, transitStep, walkToDestination };
        var estimatedWalking = walkToStop.IsEstimated || walkToDestination.IsEstimated;
        var warnings = new List<string>
        {
            "Bu plan yalnızca aktarmasız ve güzergahın ileri yönündeki yolculukları kapsar.",
            "Gerçek sefer tarifesi, bekleme süresi ve araç geliş zamanı hesaba katılmamıştır."
        };
        if (estimatedWalking) warnings.Add("Yürüyüş geometrileri doğrudan mesafe üzerinden tahmin edilmiştir.");
        return new(true, null, best.Boarding.RouteId, best.RouteName, best.RouteColor,
            ToStopDto(best.Boarding), ToStopDto(best.Alighting),
            steps.Sum(x => x.DistanceMeters), steps.Sum(x => x.DurationSeconds),
            walkToStop.DistanceMeters + walkToDestination.DistanceMeters, stopCount,
            steps, warnings, Array.Empty<string>());
    }

    private async Task<List<JourneyStopCandidateRow>> FindCandidatesAsync(
        double longitude, double latitude, double maximumDistance, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT s."Id" AS "StopId", s."Name" AS "StopName", s."RouteId", r."Name" AS "RouteName",
                   s."SequenceNumber", ST_X(s."Geometry") AS "Longitude", ST_Y(s."Geometry") AS "Latitude",
                   ST_Distance(s."Geometry"::geography, ST_SetSRID(ST_MakePoint(@longitude, @latitude), 4326)::geography) AS "WalkingDistanceMeters"
            FROM "TblTransportStops" AS s
            INNER JOIN "TblTransportRoutes" AS r ON r."Id" = s."RouteId"
            WHERE s."IsActive" AND NOT s."IsDeleted" AND r."IsActive" AND NOT r."IsDeleted"
              AND s."Geometry" && ST_MakeEnvelope(@minLongitude, @minLatitude, @maxLongitude, @maxLatitude, 4326)
              AND ST_DWithin(s."Geometry"::geography, ST_SetSRID(ST_MakePoint(@longitude, @latitude), 4326)::geography, @maximumDistance)
            ORDER BY "WalkingDistanceMeters", s."Id"
            LIMIT @candidateLimit
            """;
        var latitudeDelta = maximumDistance / 110_574d;
        var longitudeDelta = maximumDistance / (111_320d * Math.Max(0.01, Math.Cos(latitude * Math.PI / 180d)));
        return await _db.Database.SqlQueryRaw<JourneyStopCandidateRow>(sql,
            new NpgsqlParameter("longitude", longitude), new NpgsqlParameter("latitude", latitude),
            new NpgsqlParameter("minLongitude", Math.Max(-180, longitude - longitudeDelta)),
            new NpgsqlParameter("maxLongitude", Math.Min(180, longitude + longitudeDelta)),
            new NpgsqlParameter("minLatitude", Math.Max(-90, latitude - latitudeDelta)),
            new NpgsqlParameter("maxLatitude", Math.Min(90, latitude + latitudeDelta)),
            new NpgsqlParameter("maximumDistance", maximumDistance),
            new NpgsqlParameter("candidateLimit", _options.CandidateStopLimit))
            .AsNoTracking().ToListAsync(cancellationToken);
    }

    private (LineString Geometry, double DistanceMeters, double DurationSeconds)? TryBuildTransit(
        LineString routeGeometry, JourneyStopCandidateRow start, JourneyStopCandidateRow destination,
        double? routeDistance, double? routeDuration)
    {
        if (routeGeometry.SRID is not (0 or 4326) || routeGeometry.NumPoints < 2) return null;
        var indexed = new LengthIndexedLine(routeGeometry);
        var startCoordinate = new Coordinate(start.Longitude, start.Latitude);
        var destinationCoordinate = new Coordinate(destination.Longitude, destination.Latitude);
        var startIndex = indexed.Project(startCoordinate);
        var destinationIndex = indexed.Project(destinationCoordinate);
        if (startIndex >= destinationIndex) return null;
        if (VehicleSimulationGeometry.DistanceMeters(startCoordinate, indexed.ExtractPoint(startIndex)) > MaximumStopRouteDistanceMeters
            || VehicleSimulationGeometry.DistanceMeters(destinationCoordinate, indexed.ExtractPoint(destinationIndex)) > MaximumStopRouteDistanceMeters) return null;
        if (indexed.ExtractLine(startIndex, destinationIndex) is not LineString segment || segment.NumPoints < 2) return null;
        segment.SRID = 4326;
        var distance = GeodesicLength(segment);
        if (!double.IsFinite(distance) || distance <= 0) return null;
        var seconds = routeDistance is > 0 && routeDuration is > 0
            ? distance * routeDuration.Value / routeDistance.Value
            : distance / (_options.TransitAverageSpeedKmh / 3.6);
        return (segment, distance, seconds);
    }

    private async Task<JourneyStepDto> BuildWalkingStepAsync(int order, string type, string title,
        Point start, Point destination, double directDistance, CancellationToken cancellationToken)
    {
        if (_options.UseOsrmWalkingProfile && !string.IsNullOrWhiteSpace(_options.WalkingOsrmBaseUrl))
        {
            try
            {
                var route = await _osrm.GetWalkingRouteAsync(start, destination, _options.WalkingOsrmBaseUrl, cancellationToken);
                return new(order, type, title, $"Yaklaşık {Math.Round(route.Distance):N0} metre yürüyün.",
                    route.Distance, route.Duration, new(route.Geometry.Type, route.Geometry.Coordinates.Select(x => x.ToArray()).ToArray()), false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex) { _logger.LogWarning(ex, "Walking OSRM failed; direct estimated geometry will be used."); }
        }
        var geometry = new LineString([start.Coordinate.Copy(), destination.Coordinate.Copy()]) { SRID = 4326 };
        return new(order, type, title, $"Yaklaşık {Math.Round(directDistance):N0} metre yürüyün.",
            directDistance, directDistance / _options.WalkingSpeedMetersPerSecond, ToGeoJson(geometry), true);
    }

    private static int CandidateOrder(JourneyCandidate left, JourneyCandidate right)
    {
        var comparison = left.TotalSeconds.CompareTo(right.TotalSeconds);
        if (comparison != 0) return comparison;
        comparison = left.TotalWalkingDistance.CompareTo(right.TotalWalkingDistance);
        return comparison != 0 ? comparison : left.StopCount.CompareTo(right.StopCount);
    }

    private static double GeodesicLength(LineString line) => Enumerable.Range(0, line.NumPoints - 1)
        .Sum(index => VehicleSimulationGeometry.DistanceMeters(line.GetCoordinateN(index), line.GetCoordinateN(index + 1)));
    private static Point Point(double longitude, double latitude) => new(longitude, latitude) { SRID = 4326 };
    private static JourneyGeometryDto ToGeoJson(LineString line) =>
        new("LineString", line.Coordinates.Select(x => new[] { x.X, x.Y }).ToArray());
    private static JourneyStopDto ToStopDto(JourneyStopCandidateRow stop) => new(stop.StopId, stop.StopName,
        stop.RouteId, stop.RouteName, stop.SequenceNumber, stop.Longitude, stop.Latitude, stop.WalkingDistanceMeters);
    private static JourneyPlanResponse NotFound() => new(false,
        "Belirlenen yürüme mesafesi içinde aktarmasız uygun bir güzergah bulunamadı.",
        null, null, null, null, null, 0, 0, 0, 0, Array.Empty<JourneyStepDto>(), Array.Empty<string>(),
        ["Maksimum yürüme mesafesini artırabilirsiniz.", "Başlangıç veya hedef konumunu değiştirebilirsiniz."]);

    private sealed record JourneyCandidate(JourneyStopCandidateRow Boarding, JourneyStopCandidateRow Alighting,
        string RouteName, string RouteColor, LineString TransitGeometry, double TransitDistanceMeters,
        double TransitDurationSeconds, double WalkingDurationSeconds)
    {
        public double TotalSeconds => TransitDurationSeconds + WalkingDurationSeconds;
        public double TotalWalkingDistance => Boarding.WalkingDistanceMeters + Alighting.WalkingDistanceMeters;
        public int StopCount => Alighting.SequenceNumber - Boarding.SequenceNumber;
    }
}

public sealed class JourneyStopCandidateRow
{
    public int StopId { get; set; }
    public string StopName { get; set; } = "";
    public int RouteId { get; set; }
    public string RouteName { get; set; } = "";
    public int SequenceNumber { get; set; }
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public double WalkingDistanceMeters { get; set; }
}
