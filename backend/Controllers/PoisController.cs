using backend.Data;
using backend.DTOs;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.Security.Claims;

namespace backend.Controllers;
[Authorize, ApiController, Route("api/pois")]
public class PoisController : ControllerBase
{
    private readonly AppDbContext _db; private readonly IUserPermissionService _permissions;
    public PoisController(AppDbContext db, IUserPermissionService permissions) { _db = db; _permissions = permissions; }
    [HttpGet("/api/poi/search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int limit = 10)
    {
        var uid = UserId(); if (uid == null) return Unauthorized();
        if (!await _permissions.HasPermissionAsync(uid.Value, "Poi.Search")) return Forbid();
        var text = q?.Trim() ?? ""; if (text.Length < 2) return BadRequest("Arama metni en az 2 karakter olmalıdır.");
        limit = Math.Clamp(limit, 1, 25);
        var pattern = "%" + text + "%";
        var results = await _db.TblPois
            .Where(x => x.IsActive && !x.IsDeleted && x.Category.IsActive && !x.Category.IsDeleted)
            .Where(x => EF.Functions.ILike(x.Name, pattern) || EF.Functions.ILike(x.Category.Name, pattern) || (x.Category.Parent != null && EF.Functions.ILike(x.Category.Parent.Name, pattern)))
            .OrderBy(x => x.Name).Take(limit)
            .Select(x => new PoiSearchResponse { Id = x.Id, Name = x.Name, CategoryId = x.CategoryId, CategoryName = x.Category.Name, ParentCategoryName = x.Category.Parent == null ? null : x.Category.Parent.Name, WorkingHours = x.WorkingHours, IconKey = x.IconKey, Longitude = x.Geometry.X, Latitude = x.Geometry.Y })
            .AsNoTracking().ToListAsync();
        return Ok(results);
    }
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? name, [FromQuery] int? categoryId, [FromQuery] bool includeDeleted = false)
    {
        var uid = UserId(); if (uid == null) return Unauthorized();
        var searching = !string.IsNullOrWhiteSpace(name) || categoryId.HasValue;
        if (!await _permissions.HasPermissionAsync(uid.Value, searching ? "Poi.Search" : "Poi.View") && !await _permissions.HasPermissionAsync(uid.Value, "Poi.Manage")) return Forbid();
        var manage = await _permissions.HasPermissionAsync(uid.Value, "Poi.Manage");
        // POI ortak CBS verisidir; UserId yalnızca audit/ekleyen kullanıcı bilgisidir.
        var query = _db.TblPois.Include(x => x.Category).ThenInclude(x => x.Parent).Include(x => x.User).AsNoTracking().AsQueryable();
        if (!includeDeleted || !manage) query = query.Where(x => !x.IsDeleted && x.IsActive);
        if (!string.IsNullOrWhiteSpace(name)) query = query.Where(x => EF.Functions.ILike(x.Name, "%" + name.Trim() + "%"));
        if (categoryId.HasValue) query = query.Where(x => x.CategoryId == categoryId || x.Category.ParentId == categoryId);
        return Ok((await query.OrderByDescending(x => x.CreatedDate).ToListAsync()).Select(ToResponse));
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var uid = UserId(); if (uid == null) return Unauthorized(); if (!await _permissions.HasPermissionAsync(uid.Value, "Poi.View") && !await _permissions.HasPermissionAsync(uid.Value, "Poi.Manage")) return Forbid();
        var manage = await _permissions.HasPermissionAsync(uid.Value, "Poi.Manage");
        var item = await FullQuery().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && (!x.IsDeleted || manage)); return item == null ? NotFound() : Ok(ToResponse(item));
    }
    [HttpPost]
    public async Task<IActionResult> Create(PoiRequest request)
    {
        var uid = UserId(); if (uid == null) return Unauthorized(); if (!await _permissions.HasPermissionAsync(uid.Value, "Poi.Create")) return Forbid();
        var validation = await Validate(request); if (validation.Error != null) return BadRequest(validation.Error);
        var iconKey = await _db.TblPoiCategories.Where(x => x.Id == request.CategoryId).Select(x => x.IconKey).SingleAsync();
        var item = new TblPoi { Name = request.Name.Trim(), CategoryId = request.CategoryId, WorkingHours = request.WorkingHours?.Trim() ?? "", IconKey = iconKey, Geometry = validation.Point!, UserId = uid.Value, IsActive = true };
        _db.Add(item); await _db.SaveChangesAsync(); return CreatedAtAction(nameof(Detail), new { id = item.Id }, new { item.Id });
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, PoiRequest request)
    {
        var uid = UserId(); if (uid == null) return Unauthorized(); if (!await _permissions.HasPermissionAsync(uid.Value, "Poi.Update") && !await _permissions.HasPermissionAsync(uid.Value, "Poi.Manage")) return Forbid();
        var item = await _db.TblPois.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted); if (item == null) return NotFound();
        if (item.UserId != uid && !await _permissions.HasPermissionAsync(uid.Value, "Poi.Manage")) return Forbid();
        var validation = await Validate(request); if (validation.Error != null) return BadRequest(validation.Error);
        item.Name = request.Name.Trim(); item.CategoryId = request.CategoryId; item.WorkingHours = request.WorkingHours?.Trim() ?? ""; item.IconKey = await _db.TblPoiCategories.Where(x => x.Id == request.CategoryId).Select(x => x.IconKey).SingleAsync(); item.Geometry = validation.Point!; item.IsActive = request.IsActive; item.ModifiedUserId = uid; item.ModifiedDate = DateTime.UtcNow; await _db.SaveChangesAsync(); return NoContent();
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var uid = UserId(); if (uid == null) return Unauthorized(); if (!await _permissions.HasPermissionAsync(uid.Value, "Poi.Delete") && !await _permissions.HasPermissionAsync(uid.Value, "Poi.Manage")) return Forbid();
        var item = await _db.TblPois.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted); if (item == null) return NotFound();
        if (item.UserId != uid && !await _permissions.HasPermissionAsync(uid.Value, "Poi.Manage")) return Forbid();
        item.IsDeleted = true; item.IsActive = false; item.ModifiedUserId = uid; item.ModifiedDate = DateTime.UtcNow; await _db.SaveChangesAsync(); return NoContent();
    }
    private IQueryable<TblPoi> FullQuery() => _db.TblPois.Include(x => x.Category).ThenInclude(x => x.Parent).Include(x => x.User);
    private async Task<(Point? Point, string? Error)> Validate(PoiRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return (null, "POI adı zorunludur.");
        if (!await _db.TblPoiCategories.AnyAsync(x => x.Id == request.CategoryId && x.IsActive && !x.IsDeleted)) return (null, "Kategori aktif ve silinmemiş olmalıdır.");
        try { var geometry = new WKTReader().Read(request.Wkt); if (geometry is not Point point) return (null, "Geometry Point olmalıdır."); point.SRID = 4326; return (point, null); } catch { return (null, "Geçersiz WKT geometry."); }
    }
    private int? UserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private static PoiResponse ToResponse(TblPoi x) => new() { Id = x.Id, Name = x.Name, CategoryId = x.CategoryId, CategoryName = x.Category.Name, ParentCategoryName = x.Category.Parent?.Name, WorkingHours = x.WorkingHours, IconKey = x.IconKey, Wkt = new WKTWriter().Write(x.Geometry), UserId = x.UserId, Username = x.User.Username, CreatedDate = x.CreatedDate, IsActive = x.IsActive, IsDeleted = x.IsDeleted };
}
