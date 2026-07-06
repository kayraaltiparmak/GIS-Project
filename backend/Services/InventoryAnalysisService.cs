using backend.Data;
using backend.DTOs;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace backend.Services;

public class InventoryAnalysisService : IInventoryAnalysisService
{
    private readonly AppDbContext _context;

    public InventoryAnalysisService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<InventoryAnalysisResponse?> AnalyzeAsync(string wkt, int userId)
    {
        var polygon = ReadPolygon(wkt);

        if (polygon == null)
        {
            return null;
        }

        var pointCount = await _context.TblPoints
            .CountAsync(point => point.UserId == userId && point.IsActive && !point.IsDeleted && polygon.Intersects(point.Geometry));

        var lineCount = await _context.TblLines
            .CountAsync(line => line.UserId == userId && line.IsActive && !line.IsDeleted && polygon.Intersects(line.Geometry));

        var polygonCount = await _context.TblPolygons
            .CountAsync(item => item.UserId == userId && item.IsActive && !item.IsDeleted && polygon.Intersects(item.Geometry));

        return new InventoryAnalysisResponse
        {
            PointCount = pointCount,
            LineCount = lineCount,
            PolygonCount = polygonCount
        };
    }

    private static Polygon? ReadPolygon(string wkt)
    {
        if (string.IsNullOrWhiteSpace(wkt))
        {
            return null;
        }

        try
        {
            var geometry = new WKTReader().Read(wkt);
            geometry.SRID = 4326;

            return geometry as Polygon;
        }
        catch
        {
            return null;
        }
    }
}
