namespace backend.Services;
public sealed class ThematicMapOptions { public const string SectionName="ThematicMap"; public int GridCellSizeMeters { get; set; }=300; public int MaximumGridCellCount { get; set; }=2500; public double WalkingSpeedMetersPerSecond { get; set; }=1.4; }
