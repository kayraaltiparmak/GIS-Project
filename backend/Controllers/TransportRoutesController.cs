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
    private readonly ITransportRouteCalculationService _routeCalculation;
    private readonly ILogger<TransportRoutesController> _logger;
    public TransportRoutesController(AppDbContext db, IUserPermissionService permissions, ITransportRouteCalculationService routeCalculation, ILogger<TransportRoutesController> logger) =>
        (_db, _permissions, _routeCalculation, _logger) = (db, permissions, routeCalculation, logger);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        try
        {
            var uid = UserId(); if (uid == null) return Unauthorized(); if (!await Allowed(uid.Value, "TransportRoute.View")) return Forbid();
            var routes = await _db.TblTransportRoutes.Where(x => x.IsActive && !x.IsDeleted).OrderBy(x => x.Name)
                .Include(x => x.Stops).AsNoTracking().ToListAsync(cancellationToken);
            var items = routes.Select(ToResponse).ToList();
            return Ok(items);
        }
        catch (Exception ex) { return Error(ex); }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        try
        {
            var uid = UserId(); if (uid == null) return Unauthorized(); if (!await Allowed(uid.Value, "TransportRoute.View")) return Forbid();
            var route = await _db.TblTransportRoutes.Include(x => x.Stops).AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive && !x.IsDeleted, cancellationToken);
            if (route == null) return NotFound();
            var response = ToResponse(route);
            return Ok(new TransportRouteDetailResponse
            {
                Id = response.Id, Name = response.Name, Color = response.Color, CreatedDate = response.CreatedDate,
                IsActive = response.IsActive, StopCount = response.StopCount, Geometry = response.Geometry,
                DistanceMeters = response.DistanceMeters, DurationSeconds = response.DurationSeconds,
                RouteCalculatedAt = response.RouteCalculatedAt, RouteCalculationStatus = response.RouteCalculationStatus,
                Stops = route.Stops.Where(s => !s.IsDeleted).OrderBy(s => s.SequenceNumber).Select(s => new TransportStopResponse
                {
                    Id = s.Id, Name = s.Name, Geometry = s.Geometry.AsText(), RouteId = s.RouteId,
                    RouteName = route.Name, RouteColor = route.Color, SequenceNumber = s.SequenceNumber, IsActive = s.IsActive
                }).ToList()
            });
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

    [HttpPost("{routeId:int}/calculate")]
    public async Task<IActionResult> Calculate(int routeId, CancellationToken cancellationToken)
    {
        try
        {
            var uid = UserId(); if (uid == null) return Unauthorized();
            if (!await Allowed(uid.Value, "TransportRoute.Calculate")) return Forbid();
            if (!await _db.TblTransportRoutes.AnyAsync(x => x.Id == routeId && x.IsActive && !x.IsDeleted, cancellationToken))
                return NotFound("Guzergah bulunamadi.");
            var result = await _routeCalculation.CalculateAsync(routeId, cancellationToken);
            return result.Success ? Ok(result.Response) : BadRequest(result.Error);
        }
        catch (OperationCanceledException) { return StatusCode(499); }
        catch (Exception ex) { return Error(ex); }
    }

    [HttpPut("/api/transport-routes/{routeId:int}/reorder-stops")]
    public async Task<IActionResult> ReorderStops(int routeId, ReorderTransportStopsRequest request, CancellationToken cancellationToken)
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
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            var calculation = await _routeCalculation.CalculateAsync(routeId, cancellationToken);
            return Ok(new { reordered = true, routeCalculationStatus = calculation.Success ? "Success" : "Failed", route = calculation.Response, routeCalculationError = calculation.Error });
        }
        catch (Exception ex) { return Error(ex); }
    }

    private int? UserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private Task<bool> Allowed(int userId, string code) => _permissions.HasPermissionAsync(userId, code);
    private static TransportRouteResponse ToResponse(TblTransportRoute route) => new()
    {
        Id = route.Id, Name = route.Name, Color = route.Color, CreatedDate = route.CreatedDate,
        IsActive = route.IsActive, StopCount = route.Stops.Count(s => !s.IsDeleted),
        Geometry = route.Geometry == null ? null : new
        {
            type = "LineString",
            coordinates = route.Geometry.Coordinates.Select(c => new[] { c.X, c.Y }).ToArray()
        },
        DistanceMeters = route.DistanceMeters, DurationSeconds = route.DurationSeconds,
        RouteCalculatedAt = route.RouteCalculatedAt, RouteCalculationStatus = route.RouteCalculationStatus
    };
    private IActionResult Error(Exception ex) { _logger.LogError(ex, "Transport route endpoint failed."); return StatusCode(500, "Islem sirasinda beklenmeyen bir hata olustu."); }
}
