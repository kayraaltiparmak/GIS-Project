namespace backend.DTOs;

public class InventoryAnalysisResponse
{
    public int PointCount { get; set; }

    public int LineCount { get; set; }

    public int PolygonCount { get; set; }

    public int TotalCount => PointCount + LineCount + PolygonCount;
}
