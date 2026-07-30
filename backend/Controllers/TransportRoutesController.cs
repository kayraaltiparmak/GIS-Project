using backend.Data;
using backend.DTOs;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace backend.Controllers;

[Authorize, ApiController, Route("api/transport/routes")]
public class TransportRoutesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IUserPermissionService _permissions;
    private readonly ILogger<TransportRoutesController> _logger;
    public TransportRoutesController(AppDbContext db, IUserPermissionService permissions, ILogger<TransportRoutesController> logger) => (_db, _permissions, _logger) = (db, permissions, logger);

    [HttpGet]
    public async Task<IActionResult> List()
    {
        try
        {
            var uid = UserId(); if (uid == null) return Unauthorized(); if (!await Allowed(uid.Value, "TransportRoute.View")) return Forbid();
            var items = await _db.TblTransportRoutes.Where(x => x.IsActive && !x.IsDeleted).OrderBy(x => x.Name)
                .Select(x => new TransportRouteResponse { Id = x.Id, Name = x.Name, Color = x.Color, CreatedDate = x.CreatedDate, IsActive = x.IsActive, StopCount = x.Stops.Count(s => !s.IsDeleted) }).AsNoTracking().ToListAsync();
            return Ok(items);
        }
        catch (Exception ex) { return Error(ex); }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        try
        {
            var uid = UserId(); if (uid == null) return Unauthorized(); if (!await Allowed(uid.Value, "TransportRoute.View")) return Forbid();
            var item = await _db.TblTransportRoutes.Where(x => x.Id == id && x.IsActive && !x.IsDeleted)
                .Select(x => new TransportRouteDetailResponse { Id = x.Id, Name = x.Name, Color = x.Color, CreatedDate = x.CreatedDate, IsActive = x.IsActive, StopCount = x.Stops.Count(s => !s.IsDeleted), Stops = x.Stops.Where(s => !s.IsDeleted).OrderBy(s => s.SequenceNumber).Select(s => new TransportStopResponse { Id = s.Id, Name = s.Name, Geometry = s.Geometry.AsText(), RouteId = s.RouteId, RouteName = x.Name, SequenceNumber = s.SequenceNumber, IsActive = s.IsActive }).ToList() }).AsNoTracking().FirstOrDefaultAsync();
            return item == null ? NotFound() : Ok(item);
        }
        catch (Exception ex) { return Error(ex); }
    }

    [HttpPost]
    public async Task<IActionResult> Create(TransportRouteRequest request)
    {
        try
        {
            var uid = UserId(); if (uid == null) return Unauthorized(); if (!await Allowed(uid.Value, "TransportRoute.Create")) return Forbid();
            if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest("Route Name bos olamaz.");
            var item = new TblTransportRoute { Name = request.Name.Trim(), Color = request.Color.ToUpperInvariant(), CreatedUserId = uid.Value, IsActive = request.IsActive };
            _db.TblTransportRoutes.Add(item); await _db.SaveChangesAsync(); return CreatedAtAction(nameof(Detail), new { id = item.Id }, new { item.Id });
        }
        catch (Exception ex) { return Error(ex); }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, TransportRouteRequest request)
    {
        try
        {
            var uid = UserId(); if (uid == null) return Unauthorized(); if (!await Allowed(uid.Value, "TransportRoute.Update")) return Forbid();
            if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest("Route Name bos olamaz.");
            var item = await _db.TblTransportRoutes.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted); if (item == null) return NotFound();
            item.Name = request.Name.Trim(); item.Color = request.Color.ToUpperInvariant(); item.IsActive = request.IsActive; item.ModifiedDate = DateTime.UtcNow; await _db.SaveChangesAsync(); return NoContent();
        }
        catch (Exception ex) { return Error(ex); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var uid = UserId(); if (uid == null) return Unauthorized(); if (!await Allowed(uid.Value, "TransportRoute.Delete")) return Forbid();
            var item = await _db.TblTransportRoutes.Include(x => x.Stops).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted); if (item == null) return NotFound();
            item.IsDeleted = true; item.IsActive = false; item.ModifiedDate = DateTime.UtcNow;
            foreach (var stop in item.Stops.Where(x => !x.IsDeleted)) { stop.IsDeleted = true; stop.IsActive = false; stop.ModifiedDate = DateTime.UtcNow; }
            await _db.SaveChangesAsync(); return NoContent();
        }
        catch (Exception ex) { return Error(ex); }
    }

    [HttpPut("/api/transport-routes/{routeId:int}/reorder-stops")]
    public async Task<IActionResult> ReorderStops(int routeId, ReorderTransportStopsRequest request)
    {
        try
        {
            var uid = UserId(); if (uid == null) return Unauthorized();
            if (!await Allowed(uid.Value, "TransportRoute.Update") && !await Allowed(uid.Value, "TransportStop.Update")) return Forbid();
            if (request.StopIds.Count != request.StopIds.Distinct().Count()) return BadRequest("Durak kimlikleri tekrar edemez.");
            if (!await _db.TblTransportRoutes.AnyAsync(x => x.Id == routeId && x.IsActive && !x.IsDeleted)) return NotFound("Guzergah bulunamadi.");
            var stops = await _db.TblTransportStops.Where(x => x.RouteId == routeId && !x.IsDeleted).ToListAsync();
            if (stops.Count != request.StopIds.Count || stops.Any(x => !request.StopIds.Contains(x.Id))) return BadRequest("Liste yalnizca bu guzergahin silinmemis duraklarini eksiksiz icermelidir.");
            await using var transaction = await _db.Database.BeginTransactionAsync();
            for (var index = 0; index < request.StopIds.Count; index++)
            {
                var stop = stops.Single(x => x.Id == request.StopIds[index]); stop.SequenceNumber = index + 1; stop.ModifiedDate = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync(); await transaction.CommitAsync(); return NoContent();
        }
        catch (Exception ex) { return Error(ex); }
    }

    private int? UserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private Task<bool> Allowed(int userId, string code) => _permissions.HasPermissionAsync(userId, code);
    private IActionResult Error(Exception ex) { _logger.LogError(ex, "Transport route endpoint failed."); return StatusCode(500, "Islem sirasinda beklenmeyen bir hata olustu."); }
}
