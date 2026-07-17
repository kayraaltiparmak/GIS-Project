using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

[Authorize, ApiController, Route("api/location-analysis")]
public sealed class LocationAnalysisController : ControllerBase
{
    private readonly ILocationAnalysisService _service;
    private readonly IUserPermissionService _permissions;
    public LocationAnalysisController(ILocationAnalysisService service, IUserPermissionService permissions) { _service = service; _permissions = permissions; }

    [HttpPost]
    public async Task<IActionResult> Analyze(LocationAnalysisRequest request)
    {
        var userId = UserId(); if (userId == null) return Unauthorized();
        if (!await _permissions.HasPermissionAsync(userId.Value, "LocationAnalysis.Use")) return Forbid();
        try { var result = await _service.AnalyzeAsync(request, userId.Value); return result.Error == null ? Ok(result.Response) : BadRequest(new { message = result.Error }); }
        catch { return StatusCode(500, new { message = "Analiz sırasında hata oluştu." }); }
    }

    [HttpDelete("{analysisId:guid}")]
    public async Task<IActionResult> Delete(Guid analysisId)
    {
        var userId = UserId(); if (userId == null) return Unauthorized();
        if (!await _permissions.HasPermissionAsync(userId.Value, "LocationAnalysis.Use")) return Forbid();
        var me = await _permissions.GetCurrentUserAsync(userId.Value);
        return await _service.DeactivateAsync(analysisId, userId.Value, me?.IsAdmin == true) ? NoContent() : NotFound();
    }
    private int? UserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
