using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace backend.Services;

public class TblLineService : ITblLineService
{
    private readonly AppDbContext _context;

    public TblLineService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TblLine>> GetAllAsync(int userId)
    {
        return await _context.TblLines
            .Where(line => line.UserId == userId && !line.IsDeleted)
            .ToListAsync();
    }

    public async Task<TblLine?> GetByIdAsync(int id, int userId)
    {
        return await _context.TblLines
            .FirstOrDefaultAsync(line => line.Id == id && line.UserId == userId && !line.IsDeleted);
    }

    public async Task<int?> CreateAsync(string wkt, string name, string color, int userId)
    {
        var geometry = ReadGeometry<LineString>(wkt);

        if (geometry == null)
        {
            return null;
        }

        var line = new TblLine
        {
            UserId = userId,
            Name = name ?? "",
            Color = color ?? "",
            Geometry = geometry,
            IsActive = true,
            IsDeleted = false,
            ModifiedDate = DateTime.UtcNow
        };
        _context.TblLines.Add(line);
        await _context.SaveChangesAsync();

        return line.Id;
    }

    public async Task<GeometryUpdateResult> UpdateAsync(int id, string wkt, string name, string color, int userId)
    {
        var line = await _context.TblLines
            .FirstOrDefaultAsync(line => line.Id == id && line.UserId == userId && !line.IsDeleted);
        var geometry = ReadGeometry<LineString>(wkt);

        if (line == null)
        {
            return GeometryUpdateResult.NotFound;
        }

        if (geometry == null)
        {
            return GeometryUpdateResult.InvalidGeometry;
        }

        line.Name = name ?? "";
        line.Color = color ?? "";
        line.Geometry = geometry;
        line.ModifiedUserId = userId;
        line.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return GeometryUpdateResult.Success;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var line = await _context.TblLines
            .FirstOrDefaultAsync(line => line.Id == id && line.UserId == userId && !line.IsDeleted);

        if (line == null)
        {
            return false;
        }

        line.IsDeleted = true;
        line.ModifiedUserId = userId;
        line.ModifiedDate = DateTime.UtcNow;
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
