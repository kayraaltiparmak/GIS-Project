using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MapController : ControllerBase
{
    private readonly AppDbContext _context;

    public MapController(AppDbContext context)
    {
        _context = context;
    }

    [Authorize]
    [HttpGet("protected")]
    public IActionResult GetProtectedMapData()
    {
        try
        {
            return Ok(new
            {
                message = "Token gecerli. Harita ekranina erisebilirsin."
            });
        }
        catch
        {
            return StatusCode(500, "Beklenmeyen bir hata olustu.");
        }
    }

    [Authorize]
    [HttpGet("geometries")]
    public async Task<ActionResult<IEnumerable<GeometryResponse>>> QueryGeometries(
        [FromQuery] string? type,
        [FromQuery] string? name,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDirection)
    {
        try
        {
            var userId = GetUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            var normalizedType = type?.Trim();
            var normalizedName = name?.Trim();
            var results = new List<GeometryResponse>();

            if (ShouldIncludeType(normalizedType, "Point"))
            {
                var points = _context.TblPoints
                    .Where(point => point.UserId == userId.Value && !point.IsDeleted);

                if (!string.IsNullOrWhiteSpace(normalizedName))
                {
                    points = points.Where(point => EF.Functions.ILike(point.Name, $"%{normalizedName}%"));
                }

                results.AddRange((await points.ToListAsync()).Select(point => ToResponse(point, "Point", "TblPoint", point.Geometry)));
            }

            if (ShouldIncludeType(normalizedType, "Line"))
            {
                var lines = _context.TblLines
                    .Where(line => line.UserId == userId.Value && !line.IsDeleted);

                if (!string.IsNullOrWhiteSpace(normalizedName))
                {
                    lines = lines.Where(line => EF.Functions.ILike(line.Name, $"%{normalizedName}%"));
                }

                results.AddRange((await lines.ToListAsync()).Select(line => ToResponse(line, "Line", "TblLine", line.Geometry)));
            }

            if (ShouldIncludeType(normalizedType, "Polygon"))
            {
                var polygons = _context.TblPolygons
                    .Where(polygon => polygon.UserId == userId.Value && !polygon.IsDeleted);

                if (!string.IsNullOrWhiteSpace(normalizedName))
                {
                    polygons = polygons.Where(polygon => EF.Functions.ILike(polygon.Name, $"%{normalizedName}%"));
                }

                results.AddRange((await polygons.ToListAsync()).Select(polygon => ToResponse(polygon, "Polygon", "TblPolygon", polygon.Geometry)));
            }

            return SortResults(results, sortBy, sortDirection).ToList();
        }
        catch
        {
            return StatusCode(500, "Beklenmeyen bir hata olustu.");
        }
    }

    private int? GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(userIdValue, out var userId) ? userId : null;
    }

    private static bool ShouldIncludeType(string? requestedType, string type)
    {
        return string.IsNullOrWhiteSpace(requestedType) ||
               string.Equals(requestedType, type, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<GeometryResponse> SortResults(IEnumerable<GeometryResponse> results, string? sortBy, string? sortDirection)
    {
        var isDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();

        return normalizedSortBy switch
        {
            "modifieddate" => isDescending
                ? results.OrderByDescending(item => item.ModifiedDate)
                : results.OrderBy(item => item.ModifiedDate),
            "id" => isDescending
                ? results.OrderByDescending(item => item.Id)
                : results.OrderBy(item => item.Id),
            _ => isDescending
                ? results.OrderByDescending(item => item.Name)
                : results.OrderBy(item => item.Name)
        };
    }

    private static GeometryResponse ToResponse(TblPoint point, string type, string endpoint, Geometry geometry)
    {
        return new GeometryResponse
        {
            Id = point.Id,
            Type = type,
            Endpoint = endpoint,
            Name = point.Name,
            Color = point.Color,
            ModifiedDate = point.ModifiedDate,
            Wkt = new WKTWriter().Write(geometry)
        };
    }

    private static GeometryResponse ToResponse(TblLine line, string type, string endpoint, Geometry geometry)
    {
        return new GeometryResponse
        {
            Id = line.Id,
            Type = type,
            Endpoint = endpoint,
            Name = line.Name,
            Color = line.Color,
            ModifiedDate = line.ModifiedDate,
            Wkt = new WKTWriter().Write(geometry)
        };
    }

    private static GeometryResponse ToResponse(TblPolygon polygon, string type, string endpoint, Geometry geometry)
    {
        return new GeometryResponse
        {
            Id = polygon.Id,
            Type = type,
            Endpoint = endpoint,
            Name = polygon.Name,
            Color = polygon.Color,
            ModifiedDate = polygon.ModifiedDate,
            Wkt = new WKTWriter().Write(geometry)
        };
    }
}
