using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace backend.Controllers;

public class GeometryRequest
{
    public string Wkt { get; set; } = "";
}

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TblPointController : ControllerBase
{
    private readonly AppDbContext _context;

    public TblPointController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TblPoint>>> GetAll()
    {
        return await _context.TblPoints.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TblPoint>> GetById(int id)
    {
        var point = await _context.TblPoints.FindAsync(id);

        if (point == null)
        {
            return NotFound();
        }

        return point;
    }

    [HttpPost]
    public async Task<ActionResult> Create(GeometryRequest request)
    {
        var geometry = ReadGeometry<Point>(request.Wkt);

        if (geometry == null)
        {
            return BadRequest();
        }

        var point = new TblPoint { Geometry = geometry };
        _context.TblPoints.Add(point);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = point.Id }, new { point.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, GeometryRequest request)
    {
        var point = await _context.TblPoints.FindAsync(id);
        var geometry = ReadGeometry<Point>(request.Wkt);

        if (point == null)
        {
            return NotFound();
        }

        if (geometry == null)
        {
            return BadRequest();
        }

        point.Geometry = geometry;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var point = await _context.TblPoints.FindAsync(id);

        if (point == null)
        {
            return NotFound();
        }

        _context.TblPoints.Remove(point);
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
