using System.Collections.Concurrent;
using backend.Data;
using backend.DTOs;
using backend.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace backend.Services;

public sealed class VehicleSimulationService : IVehicleSimulationService, IHostedService
{
    private readonly ConcurrentDictionary<int, SimulationEntry> _simulations = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<VehicleSimulationHub> _hub;
    private readonly VehicleSimulationOptions _options;
    private readonly ILogger<VehicleSimulationService> _logger;
    private readonly CancellationTokenSource _shutdown = new();
    private Task? _cleanupTask;

    public VehicleSimulationService(
        IServiceScopeFactory scopeFactory,
        IHubContext<VehicleSimulationHub> hub,
        IOptions<VehicleSimulationOptions> options,
        ILogger<VehicleSimulationService> logger) =>
        (_scopeFactory, _hub, _options, _logger) = (scopeFactory, hub, options.Value, logger);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cleanupTask = CleanupLoopAsync(_shutdown.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _shutdown.Cancel();
        foreach (var entry in _simulations.Values) entry.Cancellation.Cancel();
        var tasks = _simulations.Values.Select(x => x.ExecutionTask).Where(x => x != null).Cast<Task>().ToArray();
        if (_cleanupTask != null) tasks = [.. tasks, _cleanupTask];
        try { await Task.WhenAll(tasks).WaitAsync(cancellationToken); }
        catch (OperationCanceledException) { }
        foreach (var entry in _simulations.Values) entry.Cancellation.Dispose();
        _simulations.Clear();
        _shutdown.Dispose();
    }

    public async Task<VehicleSimulationStartResult> StartSimulationAsync(
        int routeId, int startedByUserId, CancellationToken cancellationToken)
    {
        if (routeId <= 0) return new(false, false, "Gecerli bir guzergah kimligi zorunludur.", null);
        var routeData = await LoadRouteAsync(routeId, cancellationToken);
        if (routeData.Error != null) return new(false, false, routeData.Error, null);

        while (_simulations.TryGetValue(routeId, out var existing))
        {
            if (ReadState(existing).IsRunning)
                return new(false, true, "Bu guzergah icin bir simulasyon zaten calisiyor.", ReadState(existing));
            if (_simulations.TryRemove(new KeyValuePair<int, SimulationEntry>(routeId, existing)))
                existing.Cancellation.Dispose();
        }

        var now = DateTime.UtcNow;
        var first = routeData.Coordinates![0];
        var state = new MutableSimulationState
        {
            RouteId = routeId, RouteName = routeData.RouteName!, Longitude = first.X, Latitude = first.Y,
            ProgressPercent = 0, Status = VehicleSimulationStatuses.Running, StartedAt = now, UpdatedAt = now,
            StartedByUserId = startedByUserId, CurrentSegmentIndex = 0, IsRunning = true, IsCompleted = false
        };
        var entry = new SimulationEntry(state, new CancellationTokenSource(), routeData.Coordinates, routeData.SegmentLengths!, routeData.TotalDistance);
        if (!_simulations.TryAdd(routeId, entry))
            return new(false, true, "Bu guzergah icin bir simulasyon zaten calisiyor.", GetSimulationState(routeId));

        try
        {
            var initial = ReadState(entry);
            await PublishPositionAsync(initial, cancellationToken);
            await _hub.Clients.Group(VehicleSimulationGroups.ForRoute(routeId))
                .SendAsync(VehicleSimulationEvents.Started, ToPosition(initial), cancellationToken);
            entry.ExecutionTask = RunSimulationAsync(entry);
            return new(true, false, null, initial);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vehicle simulation could not start for route {RouteId}.", routeId);
            MarkFailed(entry);
            entry.Cancellation.Dispose();
            _simulations.TryRemove(new KeyValuePair<int, SimulationEntry>(routeId, entry));
            return new(false, false, "Simulasyon baslatilamadi.", null);
        }
    }

    public Task<VehicleSimulationStopResult> StopSimulationAsync(int routeId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_simulations.TryGetValue(routeId, out var entry) || !ReadState(entry).IsRunning)
            return Task.FromResult(new VehicleSimulationStopResult(false, "Bu guzergah icin calisan bir simulasyon yok.", GetSimulationState(routeId)));
        entry.Cancellation.Cancel();
        return Task.FromResult(new VehicleSimulationStopResult(true, null, ReadState(entry)));
    }

    public VehicleSimulationStateDto? GetSimulationState(int routeId) =>
        _simulations.TryGetValue(routeId, out var entry) ? ReadState(entry) : null;

    public bool IsSimulationRunning(int routeId) => GetSimulationState(routeId)?.IsRunning == true;

    private async Task RunSimulationAsync(SimulationEntry entry)
    {
        var intervalSeconds = _options.UpdateIntervalMilliseconds / 1000d;
        var travelled = 0d;
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(entry.Cancellation.Token, _shutdown.Token);
            while (travelled < entry.TotalDistance)
            {
                await Task.Delay(_options.UpdateIntervalMilliseconds, linked.Token);
                travelled = Math.Min(entry.TotalDistance, travelled + (_options.SpeedMetersPerSecond * intervalSeconds));
                var (coordinate, segmentIndex) = VehicleSimulationGeometry.Interpolate(entry.Coordinates, entry.SegmentLengths, travelled);
                VehicleSimulationStateDto state;
                lock (entry.SyncRoot)
                {
                    entry.State.Longitude = coordinate.X;
                    entry.State.Latitude = coordinate.Y;
                    entry.State.ProgressPercent = Math.Clamp(travelled / entry.TotalDistance * 100, 0, 100);
                    entry.State.CurrentSegmentIndex = segmentIndex;
                    entry.State.UpdatedAt = DateTime.UtcNow;
                    state = entry.State.ToDto();
                }
                await PublishPositionAsync(state, linked.Token);
            }

            VehicleSimulationStateDto completed;
            lock (entry.SyncRoot)
            {
                var last = entry.Coordinates[^1];
                entry.State.Longitude = last.X; entry.State.Latitude = last.Y;
                entry.State.ProgressPercent = 100; entry.State.Status = VehicleSimulationStatuses.Completed;
                entry.State.IsRunning = false; entry.State.IsCompleted = true; entry.State.UpdatedAt = DateTime.UtcNow;
                completed = entry.State.ToDto();
            }
            await PublishPositionAsync(completed, CancellationToken.None);
            await PublishTerminalAsync(VehicleSimulationEvents.Completed, completed);
        }
        catch (OperationCanceledException)
        {
            VehicleSimulationStateDto stopped;
            lock (entry.SyncRoot)
            {
                entry.State.Status = VehicleSimulationStatuses.Stopped;
                entry.State.IsRunning = false; entry.State.IsCompleted = false; entry.State.UpdatedAt = DateTime.UtcNow;
                stopped = entry.State.ToDto();
            }
            await PublishTerminalAsync(VehicleSimulationEvents.Stopped, stopped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vehicle simulation failed for route {RouteId}.", entry.State.RouteId);
            var failed = MarkFailed(entry);
            await PublishTerminalAsync(VehicleSimulationEvents.Failed, failed);
        }
    }

    private async Task<RouteSimulationData> LoadRouteAsync(int routeId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var route = await db.TblTransportRoutes.AsNoTracking()
            .Include(x => x.Stops.Where(s => s.IsActive && !s.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == routeId && x.IsActive && !x.IsDeleted, cancellationToken);
        if (route == null) return RouteSimulationData.Fail("Guzergah bulunamadi veya aktif degil.");
        if (route.Geometry == null || route.Geometry.IsEmpty || route.Geometry.Coordinates.Length < 2)
            return RouteSimulationData.Fail("Guzergah geometrisi hesaplanmamis.");
        if (route.Geometry.SRID is not (0 or 4326))
            return RouteSimulationData.Fail("Guzergah geometrisinin koordinat sistemi desteklenmiyor.");
        var stops = route.Stops.OrderBy(x => x.SequenceNumber).ThenBy(x => x.Id).ToList();
        if (stops.Count < 2) return RouteSimulationData.Fail("Simulasyon icin en az iki aktif durak gereklidir.");

        var coordinates = route.Geometry.Coordinates.Select(x => x.Copy()).ToArray();
        var firstStop = stops[0].Geometry.Coordinate;
        if (VehicleSimulationGeometry.DistanceMeters(coordinates[^1], firstStop)
            < VehicleSimulationGeometry.DistanceMeters(coordinates[0], firstStop))
            Array.Reverse(coordinates);
        foreach (var coordinate in coordinates) VehicleSimulationGeometry.Validate(coordinate);
        var segments = Enumerable.Range(0, coordinates.Length - 1)
            .Select(index => VehicleSimulationGeometry.DistanceMeters(coordinates[index], coordinates[index + 1])).ToArray();
        var total = segments.Sum();
        if (!double.IsFinite(total) || total <= 0) return RouteSimulationData.Fail("Guzergah geometrisinin uzunlugu gecersiz.");
        return new(route.Name, coordinates, segments, total, null);
    }

    private Task PublishPositionAsync(VehicleSimulationStateDto state, CancellationToken cancellationToken) =>
        _hub.Clients.Group(VehicleSimulationGroups.ForRoute(state.RouteId))
            .SendAsync(VehicleSimulationEvents.PositionUpdated, ToPosition(state), cancellationToken);

    private Task PublishTerminalAsync(string eventName, VehicleSimulationStateDto state) =>
        _hub.Clients.Group(VehicleSimulationGroups.ForRoute(state.RouteId))
            .SendAsync(eventName, ToPosition(state), CancellationToken.None);

    private static VehiclePositionDto ToPosition(VehicleSimulationStateDto state) =>
        new(state.RouteId, state.RouteName, state.Longitude, state.Latitude, state.ProgressPercent,
            state.Status, state.StartedAt, state.UpdatedAt);

    private static VehicleSimulationStateDto ReadState(SimulationEntry entry)
    {
        lock (entry.SyncRoot) return entry.State.ToDto();
    }

    private static VehicleSimulationStateDto MarkFailed(SimulationEntry entry)
    {
        lock (entry.SyncRoot)
        {
            entry.State.Status = VehicleSimulationStatuses.Failed;
            entry.State.IsRunning = false; entry.State.IsCompleted = false; entry.State.UpdatedAt = DateTime.UtcNow;
            return entry.State.ToDto();
        }
    }

    private async Task CleanupLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var cutoff = DateTime.UtcNow.AddMinutes(-_options.CompletedStateRetentionMinutes);
                foreach (var pair in _simulations)
                {
                    var state = ReadState(pair.Value);
                    if (!state.IsRunning && state.UpdatedAt <= cutoff
                        && _simulations.TryRemove(new KeyValuePair<int, SimulationEntry>(pair.Key, pair.Value)))
                        pair.Value.Cancellation.Dispose();
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    private sealed class SimulationEntry
    {
        public SimulationEntry(MutableSimulationState state, CancellationTokenSource cancellation,
            Coordinate[] coordinates, double[] segmentLengths, double totalDistance) =>
            (State, Cancellation, Coordinates, SegmentLengths, TotalDistance) =
            (state, cancellation, coordinates, segmentLengths, totalDistance);
        public object SyncRoot { get; } = new();
        public MutableSimulationState State { get; }
        public CancellationTokenSource Cancellation { get; }
        public Coordinate[] Coordinates { get; }
        public double[] SegmentLengths { get; }
        public double TotalDistance { get; }
        public Task? ExecutionTask { get; set; }
    }

    private sealed class MutableSimulationState
    {
        public int RouteId { get; set; }
        public string RouteName { get; set; } = "";
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double ProgressPercent { get; set; }
        public string Status { get; set; } = "";
        public DateTime StartedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int StartedByUserId { get; set; }
        public int CurrentSegmentIndex { get; set; }
        public bool IsRunning { get; set; }
        public bool IsCompleted { get; set; }
        public VehicleSimulationStateDto ToDto() => new(RouteId, RouteName, Longitude, Latitude, ProgressPercent,
            Status, StartedAt, UpdatedAt, StartedByUserId, CurrentSegmentIndex, IsRunning, IsCompleted);
    }

    private sealed record RouteSimulationData(string? RouteName, Coordinate[]? Coordinates,
        double[]? SegmentLengths, double TotalDistance, string? Error)
    {
        public static RouteSimulationData Fail(string error) => new(null, null, null, 0, error);
    }
}
