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
public class TblLineController : ControllerBase
{
    private readonly AppDbContext _context;

    public TblLineController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TblLine>>> GetAll()
    {
        return await _context.TblLines.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TblLine>> GetById(int id)
    {
        var line = await _context.TblLines.FindAsync(id);

        if (line == null)
        {
            return NotFound();
        }

        return line;
    }

    [HttpPost]
    public async Task<ActionResult> Create(GeometryRequest request)
    {
        var geometry = ReadGeometry<LineString>(request.Wkt);

        if (geometry == null)
        {
            return BadRequest();
        }

        var line = new TblLine { Geometry = geometry };
        _context.TblLines.Add(line);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = line.Id }, new { line.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, GeometryRequest request)
    {
        var line = await _context.TblLines.FindAsync(id);
        var geometry = ReadGeometry<LineString>(request.Wkt);

        if (line == null)
        {
            return NotFound();
        }

        if (geometry == null)
        {
            return BadRequest();
        }

        line.Geometry = geometry;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var line = await _context.TblLines.FindAsync(id);

        if (line == null)
        {
            return NotFound();
        }

        _context.TblLines.Remove(line);
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
