using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InventoryAnalysisController : ControllerBase
{
    private readonly IInventoryAnalysisService _inventoryAnalysisService;

    public InventoryAnalysisController(IInventoryAnalysisService inventoryAnalysisService)
    {
        _inventoryAnalysisService = inventoryAnalysisService;
    }

    [HttpPost]
    public async Task<ActionResult<InventoryAnalysisResponse>> Analyze(InventoryAnalysisRequest request)
    {
        try
        {
            var userId = GetUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _inventoryAnalysisService.AnalyzeAsync(request.Wkt, userId.Value);

            if (result == null)
            {
                return BadRequest();
            }

            return result;
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
}
