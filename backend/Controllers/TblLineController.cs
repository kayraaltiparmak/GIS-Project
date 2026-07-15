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
public class TblLineController : ControllerBase
{
    private readonly ITblLineService _lineService;
    private readonly IUserPermissionService _userPermissionService;
    private readonly IGeographicPermissionService _geographicPermissionService;

    public TblLineController(
        ITblLineService lineService,
        IUserPermissionService userPermissionService,
        IGeographicPermissionService geographicPermissionService)
    {
        _lineService = lineService;
        _userPermissionService = userPermissionService;
        _geographicPermissionService = geographicPermissionService;
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

            var lines = await _lineService.GetAllAsync(userId.Value);

            return lines.Select(ToResponse).ToList();
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

            var line = await _lineService.GetByIdAsync(id, userId.Value);

            if (line == null)
            {
                return NotFound();
            }

            return ToResponse(line);
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

            if (!await _userPermissionService.HasPermissionAsync(userId.Value, "Line.Create"))
            {
                return Forbid();
            }

            var geographicPermission = await _geographicPermissionService.CanCreateGeometryAsync(userId.Value, request.Wkt);

            if (!geographicPermission.IsAllowed)
            {
                return StatusCode(StatusCodes.Status403Forbidden, geographicPermission.ErrorMessage);
            }

            var lineId = await _lineService.CreateAsync(request.Wkt, request.Name, request.Color, userId.Value);

            if (lineId == null)
            {
                return BadRequest();
            }

            return CreatedAtAction(nameof(GetById), new { id = lineId.Value }, new { Id = lineId.Value });
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

            var result = await _lineService.UpdateAsync(id, request.Wkt, request.Name, request.Color, userId.Value);

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

            var deleted = await _lineService.DeleteAsync(id, userId.Value);

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

    private static GeometryResponse ToResponse(TblLine line)
    {
        return new GeometryResponse
        {
            Id = line.Id,
            Type = "Line",
            Endpoint = "TblLine",
            Name = line.Name,
            Color = line.Color,
            ModifiedDate = line.ModifiedDate,
            Wkt = new WKTWriter().Write(line.Geometry)
        };
    }
}
