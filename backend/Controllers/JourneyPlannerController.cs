using System.Security.Claims;
using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace backend.Controllers;

[Authorize, ApiController, Route("api/journey-planner")]
public sealed class JourneyPlannerController : ControllerBase
{
    private const double MinimumWalkingDistanceMeters = 100;
    private readonly IJourneyPlannerService _planner;
    private readonly IUserPermissionService _permissions;
    private readonly JourneyPlannerOptions _options;
    private readonly ILogger<JourneyPlannerController> _logger;

    public JourneyPlannerController(IJourneyPlannerService planner, IUserPermissionService permissions,
        IOptions<JourneyPlannerOptions> options, ILogger<JourneyPlannerController> logger) =>
        (_planner, _permissions, _options, _logger) = (planner, permissions, options.Value, logger);

    [HttpPost("plan")]
    public async Task<IActionResult> Plan(JourneyPlanRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (int?)null;
            if (userId == null) return Unauthorized();
            if (!await _permissions.HasPermissionAsync(userId.Value, TransportPermissionCodes.JourneyPlanView)) return Forbid();
            var validationError = Validate(request);
            if (validationError != null) return BadRequest(validationError);
            return Ok(await _planner.PlanAsync(request, cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return StatusCode(499); }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Journey planning timed out.");
            return StatusCode(504, "Yolculuk planı zaman aşımına uğradı. Lütfen tekrar deneyin.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Journey planning failed.");
            return StatusCode(500, "Yolculuk planı oluşturulurken beklenmeyen bir hata oluştu.");
        }
    }

    private string? Validate(JourneyPlanRequest request)
    {
        if (!ValidLongitude(request.StartLongitude) || !ValidLongitude(request.DestinationLongitude))
            return "Boylam -180 ile 180 arasında olmalıdır.";
        if (!ValidLatitude(request.StartLatitude) || !ValidLatitude(request.DestinationLatitude))
            return "Enlem -90 ile 90 arasında olmalıdır.";
        if (request.StartLongitude == request.DestinationLongitude && request.StartLatitude == request.DestinationLatitude)
            return "Başlangıç ve hedef koordinatları aynı olamaz.";
        if (request.MaximumWalkingDistanceMeters is { } distance
            && (!double.IsFinite(distance) || distance < MinimumWalkingDistanceMeters
                || distance > _options.MaximumAllowedWalkingDistanceMeters))
            return $"Maksimum yürüme mesafesi {MinimumWalkingDistanceMeters:g} ile {_options.MaximumAllowedWalkingDistanceMeters:g} metre arasında olmalıdır.";
        return null;
    }

    private static bool ValidLongitude(double value) => double.IsFinite(value) && value is >= -180 and <= 180;
    private static bool ValidLatitude(double value) => double.IsFinite(value) && value is >= -90 and <= 90;
}
