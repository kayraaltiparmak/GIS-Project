using backend.DTOs;

namespace backend.Services;

public interface IJourneyPlannerService
{
    Task<JourneyPlanResponse> PlanAsync(JourneyPlanRequest request, CancellationToken cancellationToken);
}
