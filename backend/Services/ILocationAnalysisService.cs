using backend.DTOs;

namespace backend.Services;

public interface ILocationAnalysisService
{
    Task<LocationAnalysisServiceResult> AnalyzeAsync(LocationAnalysisRequest request, int userId);
    Task<bool> DeactivateAsync(Guid analysisId, int userId, bool isAdmin);
}
