using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace backend.Services;

public class TblPointService : ITblPointService
{
    private readonly AppDbContext _context;

    public TblPointService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TblPoint>> GetAllAsync(int userId)
    {
        return await _context.TblPoints
            .Where(point => point.UserId == userId && !point.IsDeleted)
            .ToListAsync();
    }

    public async Task<TblPoint?> GetByIdAsync(int id, int userId)
    {
        return await _context.TblPoints
            .FirstOrDefaultAsync(point => point.Id == id && point.UserId == userId && !point.IsDeleted);
    }

    public async Task<int?> CreateAsync(string wkt, string name, string color, int userId)
    {
        var geometry = ReadGeometry<Point>(wkt);

        if (geometry == null)
        {
            return null;
        }

        var point = new TblPoint
        {
            UserId = userId,
            Name = name ?? "",
            Color = color ?? "",
            Geometry = geometry,
            IsActive = true,
            IsDeleted = false,
            ModifiedDate = DateTime.UtcNow
        };
        _context.TblPoints.Add(point);
        await _context.SaveChangesAsync();

        return point.Id;
    }

    public async Task<GeometryUpdateResult> UpdateAsync(int id, string wkt, string name, string color, int userId)
    {
        var point = await _context.TblPoints
            .FirstOrDefaultAsync(point => point.Id == id && point.UserId == userId && !point.IsDeleted);
        var geometry = ReadGeometry<Point>(wkt);

        if (point == null)
        {
            return GeometryUpdateResult.NotFound;
        }

        if (geometry == null)
        {
            return GeometryUpdateResult.InvalidGeometry;
        }

        point.Name = name ?? "";
        point.Color = color ?? "";
        point.Geometry = geometry;
        point.ModifiedUserId = userId;
        point.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return GeometryUpdateResult.Success;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var point = await _context.TblPoints
            .FirstOrDefaultAsync(point => point.Id == id && point.UserId == userId && !point.IsDeleted);

        if (point == null)
        {
            return false;
        }

        point.IsDeleted = true;
        point.ModifiedUserId = userId;
        point.ModifiedDate = DateTime.UtcNow;
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
