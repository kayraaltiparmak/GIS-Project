using backend.DTOs;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.IO;
using System.Security.Claims;

namespace backend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TblPolygonController : ControllerBase
{
    private readonly ITblPolygonService _polygonService;

    public TblPolygonController(ITblPolygonService polygonService)
    {
        _polygonService = polygonService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GeometryResponse>>> GetAll()
    {
        try
        {
            var userId = GetUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            var polygons = await _polygonService.GetAllAsync(userId.Value);

            return polygons.Select(ToResponse).ToList();
        }
        catch
        {
            return StatusCode(500, "Beklenmeyen bir hata olustu.");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GeometryResponse>> GetById(int id)
    {
        try
        {
            var userId = GetUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            var polygon = await _polygonService.GetByIdAsync(id, userId.Value);

            if (polygon == null)
            {
                return NotFound();
            }

            return ToResponse(polygon);
        }
        catch
        {
            return StatusCode(500, "Beklenmeyen bir hata olustu.");
        }
    }

    [HttpPost]
    public async Task<ActionResult> Create(GeometryRequest request)
    {
        try
        {
            var userId = GetUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _polygonService.CreateAsync(request.Wkt, request.Name, request.Color, userId.Value);

            if (result == null)
            {
                return BadRequest();
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new { result.Id, result.InventoryCount });
        }
        catch
        {
            return StatusCode(500, "Beklenmeyen bir hata olustu.");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, GeometryRequest request)
    {
        try
        {
            var userId = GetUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _polygonService.UpdateAsync(id, request.Wkt, request.Name, request.Color, userId.Value);

            if (result == GeometryUpdateResult.NotFound)
            {
                return NotFound();
            }

            if (result == GeometryUpdateResult.InvalidGeometry)
            {
                return BadRequest();
            }

            return NoContent();
        }
        catch
        {
            return StatusCode(500, "Beklenmeyen bir hata olustu.");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var userId = GetUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            var deleted = await _polygonService.DeleteAsync(id, userId.Value);

            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
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

    private static GeometryResponse ToResponse(TblPolygon polygon)
    {
        return new GeometryResponse
        {
            Id = polygon.Id,
            Type = "Polygon",
            Endpoint = "TblPolygon",
            Name = polygon.Name,
            Color = polygon.Color,
            ModifiedDate = polygon.ModifiedDate,
            Wkt = new WKTWriter().Write(polygon.Geometry)
        };
    }
}
