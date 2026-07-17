using System.Text.Json;

namespace backend.DTOs;

public sealed class LocationAnalysisRequest
{
    public JsonElement TargetGeometry { get; set; }
    public List<LocationAnalysisCriterionRequest> Criteria { get; set; } = [];
}

public sealed class LocationAnalysisCriterionRequest
{
    public int CategoryId { get; set; }
    public int Weight { get; set; }
}

public sealed class LocationAnalysisResponse
{
    public Guid AnalysisId { get; set; }
    public int MatchedPoiCount { get; set; }
    public List<LocationAnalysisCriterionSummary> CriteriaSummary { get; set; } = [];
    public string Message { get; set; } = "Analiz başarıyla tamamlandı";
    public List<LocationAnalysisPointResponse> Points { get; set; } = [];
}

public sealed class LocationAnalysisPointResponse
{
    public int PoiId { get; set; }
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public double HeatWeight { get; set; }
}

public sealed class LocationAnalysisCriterionSummary
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public int Weight { get; set; }
    public int MatchedPoiCount { get; set; }
}

public sealed record LocationAnalysisServiceResult(LocationAnalysisResponse? Response, string? Error);
