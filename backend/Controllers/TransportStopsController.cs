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

[Authorize, ApiController, Route("api/transport/stops")]
public class TransportStopsController : ControllerBase
{
    private readonly AppDbContext _db; private readonly IUserPermissionService _permissions; private readonly ILogger<TransportStopsController> _logger;
    public TransportStopsController(AppDbContext db, IUserPermissionService permissions, ILogger<TransportStopsController> logger) => (_db, _permissions, _logger) = (db, permissions, logger);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? routeId)
    {
        try { var uid = UserId(); if (uid == null) return Unauthorized(); if (!await Allowed(uid.Value, "TransportStop.View")) return Forbid(); var query = _db.TblTransportStops.Where(x => x.IsActive && !x.IsDeleted && x.Route.IsActive && !x.Route.IsDeleted); if (routeId.HasValue) query = query.Where(x => x.RouteId == routeId); return Ok(await query.OrderBy(x => x.RouteId).ThenBy(x => x.SequenceNumber).Select(ToResponse()).AsNoTracking().ToListAsync()); }
        catch (Exception ex) { return Error(ex); }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        try { var uid = UserId(); if (uid == null) return Unauthorized(); if (!await Allowed(uid.Value, "TransportStop.View")) return Forbid(); var item = await _db.TblTransportStops.Where(x => x.Id == id && x.IsActive && !x.IsDeleted && x.Route.IsActive && !x.Route.IsDeleted).Select(ToResponse()).AsNoTracking().FirstOrDefaultAsync(); return item == null ? NotFound() : Ok(item); }
        catch (Exception ex) { return Error(ex); }
    }

    [HttpPost]
    public async Task<IActionResult> Create(TransportStopRequest request)
    {
        try { var uid = UserId(); if (uid == null) return Unauthorized(); if (!await Allowed(uid.Value, "TransportStop.Create")) return Forbid(); var validation = await Validate(request); if (validation.Error != null) return BadRequest(validation.Error); await using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable); var next = (await _db.TblTransportStops.Where(x => x.RouteId == request.RouteId && !x.IsDeleted).MaxAsync(x => (int?)x.SequenceNumber) ?? 0) + 1; var item = new TblTransportStop { Name = request.Name.Trim(), Geometry = validation.Point!, RouteId = request.RouteId, SequenceNumber = next, CreatedUserId = uid.Value, IsActive = request.IsActive }; _db.Add(item); await _db.SaveChangesAsync(); await tx.CommitAsync(); return CreatedAtAction(nameof(Detail), new { id = item.Id }, new { item.Id }); }
        catch (Exception ex) { return Error(ex); }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, TransportStopRequest request)
    {
        try { var uid = UserId(); if (uid == null) return Unauthorized(); if (!await Allowed(uid.Value, "TransportStop.Update")) return Forbid(); await using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable); var item = await _db.TblTransportStops.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted); if (item == null) return NotFound(); var validation = await Validate(request); if (validation.Error != null) return BadRequest(validation.Error); var oldRouteId = item.RouteId; if (oldRouteId != request.RouteId) { item.RouteId = request.RouteId; item.SequenceNumber = (await _db.TblTransportStops.Where(x => x.RouteId == request.RouteId && !x.IsDeleted).MaxAsync(x => (int?)x.SequenceNumber) ?? 0) + 1; } item.Name = request.Name.Trim(); item.Geometry = validation.Point!; item.IsActive = request.IsActive; item.ModifiedDate = DateTime.UtcNow; await _db.SaveChangesAsync(); if (oldRouteId != request.RouteId) await Normalize(oldRouteId); await _db.SaveChangesAsync(); await tx.CommitAsync(); return NoContent(); }
        catch (Exception ex) { return Error(ex); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try { var uid = UserId(); if (uid == null) return Unauthorized(); if (!await Allowed(uid.Value, "TransportStop.Delete")) return Forbid(); await using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable); var item = await _db.TblTransportStops.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted); if (item == null) return NotFound(); item.IsDeleted = true; item.IsActive = false; item.ModifiedDate = DateTime.UtcNow; await _db.SaveChangesAsync(); await Normalize(item.RouteId); await _db.SaveChangesAsync(); await tx.CommitAsync(); return NoContent(); }
        catch (Exception ex) { return Error(ex); }
    }

    private async Task<(Point? Point, string? Error)> Validate(TransportStopRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return (null, "Stop Name bos olamaz.");
        if (request.RouteId <= 0 || !await _db.TblTransportRoutes.AnyAsync(x => x.Id == request.RouteId && x.IsActive && !x.IsDeleted)) return (null, "Gecerli bir RouteId zorunludur.");
        if (request.Geometry is { } geo) { if (!geo.Type.Equals("Point", StringComparison.OrdinalIgnoreCase) || geo.Coordinates.Length != 2 || geo.Coordinates[0] is < -180 or > 180 || geo.Coordinates[1] is < -90 or > 90) return (null, "Geometry gecerli bir GeoJSON Point olmalidir."); return (new Point(geo.Coordinates[0], geo.Coordinates[1]) { SRID = 4326 }, null); }
        try { var geometry = new WKTReader().Read(request.Wkt); if (geometry is not Point point || !point.IsValid) return (null, "Geometry gecerli bir Point olmalidir."); point.SRID = 4326; return (point, null); } catch { return (null, "Geometry zorunludur."); }
    }
    private async Task Normalize(int routeId) { var items = await _db.TblTransportStops.Where(x => x.RouteId == routeId && !x.IsDeleted).OrderBy(x => x.SequenceNumber).ThenBy(x => x.Id).ToListAsync(); for (var i = 0; i < items.Count; i++) items[i].SequenceNumber = i + 1; }
    private static System.Linq.Expressions.Expression<Func<TblTransportStop, TransportStopResponse>> ToResponse() => x => new TransportStopResponse { Id = x.Id, Name = x.Name, Geometry = x.Geometry.AsText(), RouteId = x.RouteId, RouteName = x.Route.Name, RouteColor = x.Route.Color, SequenceNumber = x.SequenceNumber, IsActive = x.IsActive, CreatedUserId = x.CreatedUserId, CreatedUsername = x.CreatedUser.Username, CreatedDate = x.CreatedDate };
    private int? UserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private Task<bool> Allowed(int userId, string code) => _permissions.HasPermissionAsync(userId, code);
    private IActionResult Error(Exception ex) { _logger.LogError(ex, "Transport stop endpoint failed."); return StatusCode(500, "Islem sirasinda beklenmeyen bir hata olustu."); }
}
