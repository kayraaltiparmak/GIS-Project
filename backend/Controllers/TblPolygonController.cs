using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace backend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TblPolygonController : ControllerBase
{
    private readonly AppDbContext _context;

    public TblPolygonController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TblPolygon>>> GetAll()
    {
        return await _context.TblPolygons.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TblPolygon>> GetById(int id)
    {
        var polygon = await _context.TblPolygons.FindAsync(id);

        if (polygon == null)
        {
            return NotFound();
        }

        return polygon;
    }

    [HttpPost]
    public async Task<ActionResult> Create(GeometryRequest request)
    {
        var geometry = ReadGeometry<Polygon>(request.Wkt);

        if (geometry == null)
        {
            return BadRequest();
        }

        var polygon = new TblPolygon { Geometry = geometry };
        _context.TblPolygons.Add(polygon);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = polygon.Id }, new { polygon.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, GeometryRequest request)
    {
        var polygon = await _context.TblPolygons.FindAsync(id);
        var geometry = ReadGeometry<Polygon>(request.Wkt);

        if (polygon == null)
        {
            return NotFound();
        }

        if (geometry == null)
        {
            return BadRequest();
        }

        polygon.Geometry = geometry;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var polygon = await _context.TblPolygons.FindAsync(id);

        if (polygon == null)
        {
            return NotFound();
        }

        _context.TblPolygons.Remove(polygon);
        await _context.SaveChangesAsync();

        return NoContent();
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
