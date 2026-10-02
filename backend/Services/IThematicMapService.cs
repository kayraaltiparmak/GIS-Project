using backend.DTOs;
namespace backend.Services;
public interface IThematicMapService { Task<ThematicMapResponse<ThematicGridCellDto>> GetNearestStopDistanceAsync(ThematicExtentRequest extent,bool walkingTime,CancellationToken cancellationToken); Task<ThematicMapResponse<ThematicRouteDto>> GetRouteDensityAsync(ThematicExtentRequest extent,CancellationToken cancellationToken); }
public sealed class ThematicMapValidationException(string message):Exception(message);
