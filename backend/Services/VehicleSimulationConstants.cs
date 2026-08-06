namespace backend.Services;

public static class VehicleSimulationStatuses
{
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string Stopped = "Stopped";
    public const string Failed = "Failed";
}

public static class VehicleSimulationEvents
{
    public const string PositionUpdated = "VehiclePositionUpdated";
    public const string Started = "SimulationStarted";
    public const string Completed = "SimulationCompleted";
    public const string Stopped = "SimulationStopped";
    public const string Failed = "SimulationFailed";
    public const string SpeedChanged = "SimulationSpeedChanged";
}

public static class VehicleSimulationGroups
{
    public static string ForRoute(int routeId) => $"RouteSimulation_{routeId}";
}

public static class TransportPermissionCodes
{
    public const string RouteView = "TransportRoute.View";
    public const string SimulationStart = "TransportSimulation.Start";
    public const string SimulationStop = "TransportSimulation.Stop";
    public const string SimulationView = "TransportSimulation.View";
    public const string JourneyPlanView = "TransportJourneyPlan.View";
}
