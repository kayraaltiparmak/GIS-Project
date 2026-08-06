namespace backend.DTOs;

public sealed record JourneyPlanRequest(
    double StartLongitude,
    double StartLatitude,
    double DestinationLongitude,
    double DestinationLatitude,
    double? MaximumWalkingDistanceMeters);

public sealed record JourneyStopDto(
    int StopId,
    string StopName,
    int RouteId,
    string RouteName,
    int SequenceNumber,
    double Longitude,
    double Latitude,
    double WalkingDistanceMeters);

public sealed record JourneyGeometryDto(string Type, double[][] Coordinates);

public sealed record JourneyStepDto(
    int Order,
    string Type,
    string Title,
    string Description,
    double DistanceMeters,
    double DurationSeconds,
    JourneyGeometryDto Geometry,
    bool IsEstimated,
    int? RouteId = null,
    string? RouteName = null,
    string? RouteColor = null,
    int? BoardingStopId = null,
    int? AlightingStopId = null,
    int? StopCount = null);

public sealed record JourneyPlanResponse(
    bool Found,
    string? Message,
    int? RouteId,
    string? RouteName,
    string? RouteColor,
    JourneyStopDto? BoardingStop,
    JourneyStopDto? AlightingStop,
    double TotalDistanceMeters,
    double TotalDurationSeconds,
    double TotalWalkingDistanceMeters,
    int TransitStopCount,
    IReadOnlyList<JourneyStepDto> Steps,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Suggestions);

public static class JourneyStepTypes
{
    public const string WalkToStop = "WalkToStop";
    public const string RideTransit = "RideTransit";
    public const string WalkToDestination = "WalkToDestination";
}
