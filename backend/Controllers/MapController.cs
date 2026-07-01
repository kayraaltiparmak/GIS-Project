using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MapController : ControllerBase
{
    [Authorize]
    [HttpGet("protected")]
    public IActionResult GetProtectedMapData()
    {
        return Ok(new
        {
            message = "Token geçerli. Harita ekranına erişebilirsin."
        });
    }
}