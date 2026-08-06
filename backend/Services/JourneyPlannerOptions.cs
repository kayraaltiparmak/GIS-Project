namespace backend.Services;

public sealed class JourneyPlannerOptions
{
    public const string SectionName = "JourneyPlanner";
    public double DefaultMaximumWalkingDistanceMeters { get; set; } = 1500;
    public double MaximumAllowedWalkingDistanceMeters { get; set; } = 5000;
    public int CandidateStopLimit { get; set; } = 10;
    public double WalkingSpeedMetersPerSecond { get; set; } = 1.4;
    public bool UseOsrmWalkingProfile { get; set; }
    public string WalkingOsrmBaseUrl { get; set; } = "";
    public double TransitAverageSpeedKmh { get; set; } = 30;
}
