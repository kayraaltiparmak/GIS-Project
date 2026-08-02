namespace backend.Services;

public sealed class VehicleSimulationOptions
{
    public const string SectionName = "VehicleSimulation";
    public int UpdateIntervalMilliseconds { get; set; } = 1000;
    public double SpeedMetersPerSecond { get; set; } = 30;
    public int CompletedStateRetentionMinutes { get; set; } = 30;
}
