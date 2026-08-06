namespace backend.Services;

public sealed class VehicleSimulationOptions
{
    public const string SectionName = "VehicleSimulation";
    public int UpdateIntervalMilliseconds { get; set; } = 1000;
    public double DefaultSpeedKmh { get; set; } = 40;
    public double MinimumSpeedKmh { get; set; } = 5;
    public double MaximumSpeedKmh { get; set; } = 120;
    public int CompletedStateRetentionMinutes { get; set; } = 10;
    public double? SpeedMetersPerSecond { get; set; }
    public double EffectiveDefaultSpeedKmh =>
        SpeedMetersPerSecond is > 0 && DefaultSpeedKmh == 40
            ? SpeedMetersPerSecond.Value * 3.6
            : DefaultSpeedKmh;
}
