using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace backend.Services;

public class TblPolygonService : ITblPolygonService
{
    private readonly AppDbContext _context;

    public TblPolygonService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TblPolygon>> GetAllAsync(int userId)
    {
        return await _context.TblPolygons
            .Where(polygon => polygon.UserId == userId && !polygon.IsDeleted)
            .ToListAsync();
    }

    public async Task<TblPolygon?> GetByIdAsync(int id, int userId)
    {
        return await _context.TblPolygons
            .FirstOrDefaultAsync(polygon => polygon.Id == id && polygon.UserId == userId && !polygon.IsDeleted);
    }

    public async Task<PolygonCreateResult?> CreateAsync(string wkt, string name, string color, int userId)
    {
        var geometry = ReadGeometry<Polygon>(wkt);

        if (geometry == null)
        {
            return null;
        }

        var polygon = new TblPolygon
        {
            UserId = userId,
            Name = name ?? "",
            Color = color ?? "",
            Geometry = geometry,
            IsActive = true,
            IsDeleted = false,
            ModifiedDate = DateTime.UtcNow
        };
        _context.TblPolygons.Add(polygon);
        await _context.SaveChangesAsync();

        var inventoryCount = await _context.TblPoints
            .CountAsync(point => point.UserId == userId && !point.IsDeleted && geometry.Contains(point.Geometry));

        return new PolygonCreateResult
        {
            Id = polygon.Id,
            InventoryCount = inventoryCount
        };
    }

    public async Task<GeometryUpdateResult> UpdateAsync(int id, string wkt, string name, string color, int userId)
    {
        var polygon = await _context.TblPolygons
            .FirstOrDefaultAsync(polygon => polygon.Id == id && polygon.UserId == userId && !polygon.IsDeleted);
        var geometry = ReadGeometry<Polygon>(wkt);

        if (polygon == null)
        {
            return GeometryUpdateResult.NotFound;
        }

        if (geometry == null)
        {
            return GeometryUpdateResult.InvalidGeometry;
        }

        polygon.Name = name ?? "";
        polygon.Color = color ?? "";
        polygon.Geometry = geometry;
        polygon.ModifiedUserId = userId;
        polygon.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return GeometryUpdateResult.Success;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var polygon = await _context.TblPolygons
            .FirstOrDefaultAsync(polygon => polygon.Id == id && polygon.UserId == userId && !polygon.IsDeleted);

        if (polygon == null)
        {
            return false;
        }

        polygon.IsDeleted = true;
        polygon.ModifiedUserId = userId;
        polygon.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    private static T? ReadGeometry<T>(string wkt) where T : Geometry
    {
        if (string.IsNullOrWhiteSpace(wkt))
        {
            return null;
        }

        try
        {
            var geometry = new WKTReader().Read(wkt);
            geometry.SRID = 4326;

            return geometry as T;
        }
        catch
        {
            return null;
        }
    }
}
