using backend.DTOs;

namespace backend.Services;

public interface IInventoryAnalysisService
{
    Task<InventoryAnalysisResponse?> AnalyzeAsync(string wkt, int userId);
}
