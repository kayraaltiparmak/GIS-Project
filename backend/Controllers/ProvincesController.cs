using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.IO;

namespace backend.Controllers;

[Authorize, ApiController, Route("api/provinces")]
public sealed class ProvincesController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProvincesController(AppDbContext db) => _db = db;
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await _db.ProvinceBoundaries.Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync());
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var item = await _db.ProvinceBoundaries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        return item == null ? NotFound() : Ok(new { item.Id, item.Name, geometry = new GeoJsonWriter().Write(item.Geometry) });
    }
}
