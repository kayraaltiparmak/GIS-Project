using backend.DTOs;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.IO;
using System.Security.Claims;

namespace backend.Controllers;

public class GeometryRequest
{
    public string Wkt { get; set; } = "";

    public string Name { get; set; } = "";

    public string Color { get; set; } = "";
}

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TblPointController : ControllerBase
{
    private readonly ITblPointService _pointService;

    public TblPointController(ITblPointService pointService)
    {
        _pointService = pointService;
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

            var points = await _pointService.GetAllAsync(userId.Value);

            return points.Select(ToResponse).ToList();
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

            var point = await _pointService.GetByIdAsync(id, userId.Value);

            if (point == null)
            {
                return NotFound();
            }

            return ToResponse(point);
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

            var pointId = await _pointService.CreateAsync(request.Wkt, request.Name, request.Color, userId.Value);

            if (pointId == null)
            {
                return BadRequest();
            }

            return CreatedAtAction(nameof(GetById), new { id = pointId.Value }, new { Id = pointId.Value });
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

            var result = await _pointService.UpdateAsync(id, request.Wkt, request.Name, request.Color, userId.Value);

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

            var deleted = await _pointService.DeleteAsync(id, userId.Value);

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

    private static GeometryResponse ToResponse(TblPoint point)
    {
        return new GeometryResponse
        {
            Id = point.Id,
            Type = "Point",
            Endpoint = "TblPoint",
            Name = point.Name,
            Color = point.Color,
            ModifiedDate = point.ModifiedDate,
            Wkt = new WKTWriter().Write(point.Geometry)
        };
    }
}
