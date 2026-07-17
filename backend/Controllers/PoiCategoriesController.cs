using backend.Data;
using backend.DTOs;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace backend.Controllers;

[Authorize, ApiController, Route("api/poi-categories")]
public class PoiCategoriesController : ControllerBase
{
    private readonly AppDbContext _db; private readonly IUserPermissionService _permissions;
    public PoiCategoriesController(AppDbContext db, IUserPermissionService permissions) { _db = db; _permissions = permissions; }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool hierarchical = false, [FromQuery] bool includeDeleted = false)
    {
        var uid = UserId(); if (uid == null) return Unauthorized();
        if (!await HasAny(uid.Value, "Poi.View", "Poi.Manage", "PoiCategory.Manage")) return Forbid();
        var query = _db.TblPoiCategories.Include(x => x.Parent).AsNoTracking();
        var records = await (includeDeleted && await _permissions.HasPermissionAsync(uid.Value, "PoiCategory.Manage") ? query : query.Where(x => !x.IsDeleted)).OrderBy(x => x.Name).ToListAsync();
        var result = records.Select(ToResponse).ToList();
        if (!hierarchical) return Ok(result);
        var lookup = result.ToLookup(x => x.ParentId);
        foreach (var item in result) item.Children = lookup[item.Id].ToList();
        return Ok(lookup[null].ToList());
    }

    [HttpGet("active")]
    public async Task<IActionResult> Active()
    {
        var uid = UserId(); if (uid == null) return Unauthorized();
        if (!await HasAny(uid.Value, "Poi.View", "Poi.Create", "Poi.Manage", "PoiCategory.Manage")) return Forbid();
        var items = await _db.TblPoiCategories.Include(x => x.Parent).Where(x => x.IsActive && !x.IsDeleted).OrderBy(x => x.Name).AsNoTracking().ToListAsync();
        return Ok(items.Select(x => new { x.Id, x.Name, x.IconKey, x.ParentId, ParentName = x.Parent == null ? null : x.Parent.Name, DisplayName = x.Parent == null ? x.Name : x.Parent.Name + " > " + x.Name }));
    }

    [HttpPost]
    public async Task<IActionResult> Create(PoiCategoryRequest request)
    {
        var uid = UserId(); if (uid == null) return Unauthorized(); if (!await _permissions.HasPermissionAsync(uid.Value, "PoiCategory.Manage")) return Forbid();
        var error = await Validate(request, null); if (error != null) return BadRequest(error);
        var item = new TblPoiCategory { Name = request.Name.Trim(), Description = request.Description?.Trim() ?? "", IconKey = NormalizeIcon(request.IconKey), ParentId = request.ParentId, IsActive = request.IsActive, CreatedUserId = uid, ModifiedUserId = uid };
        _db.Add(item); await _db.SaveChangesAsync(); return CreatedAtAction(nameof(List), new { id = item.Id }, new { item.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, PoiCategoryRequest request)
    {
        var uid = UserId(); if (uid == null) return Unauthorized(); if (!await _permissions.HasPermissionAsync(uid.Value, "PoiCategory.Manage")) return Forbid();
        var item = await _db.TblPoiCategories.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted); if (item == null) return NotFound();
        var error = await Validate(request, id); if (error != null) return BadRequest(error);
        item.Name = request.Name.Trim(); item.Description = request.Description?.Trim() ?? ""; item.IconKey = NormalizeIcon(request.IconKey); item.ParentId = request.ParentId; item.IsActive = request.IsActive; item.ModifiedUserId = uid; item.ModifiedDate = DateTime.UtcNow;
        await _db.TblPois.Where(x => x.CategoryId == id).ExecuteUpdateAsync(update => update.SetProperty(x => x.IconKey, item.IconKey).SetProperty(x => x.ModifiedDate, DateTime.UtcNow));
        await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var uid = UserId(); if (uid == null) return Unauthorized(); if (!await _permissions.HasPermissionAsync(uid.Value, "PoiCategory.Manage")) return Forbid();
        var item = await _db.TblPoiCategories.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted); if (item == null) return NotFound();
        if (await _db.TblPoiCategories.AnyAsync(x => x.ParentId == id && !x.IsDeleted)) return Conflict("Bu kategori silinemez: kayıtlı alt kategorileri bulunuyor.");
        if (await _db.TblPois.AnyAsync(x => x.CategoryId == id && x.IsActive && !x.IsDeleted)) return Conflict("Bu kategori silinemez: aktif POI kayıtları bulunuyor.");
        item.IsDeleted = true; item.IsActive = false; item.ModifiedUserId = uid; item.ModifiedDate = DateTime.UtcNow; await _db.SaveChangesAsync(); return NoContent();
    }

    private async Task<string?> Validate(PoiCategoryRequest request, int? id)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return "Kategori adı zorunludur.";
        if (id.HasValue && request.ParentId == id.Value) return "Bir kategori kendisinin üst kategorisi olamaz.";
        if (request.ParentId.HasValue && !await _db.TblPoiCategories.AnyAsync(x => x.Id == request.ParentId && x.IsActive && !x.IsDeleted)) return "Üst kategori aktif ve silinmemiş olmalıdır.";
        return null;
    }
    private int? UserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private async Task<bool> HasAny(int id, params string[] codes) { foreach (var code in codes) if (await _permissions.HasPermissionAsync(id, code)) return true; return false; }
    private static string NormalizeIcon(string? key) => new[] { "default", "pharmacy", "cafe", "restaurant", "hospital", "school", "market" }.Contains(key) ? key! : "default";
    private static PoiCategoryResponse ToResponse(TblPoiCategory x) => new() { Id = x.Id, Name = x.Name, Description = x.Description, IconKey = x.IconKey, ParentId = x.ParentId, ParentName = x.Parent?.Name, IsActive = x.IsActive, IsDeleted = x.IsDeleted };
}
