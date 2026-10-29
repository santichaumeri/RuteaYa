using Microsoft.AspNetCore.Mvc;

namespace RuteaYa.Api.Controllers;

/// <summary>
/// Endpoint de prueba para verificar que la API y Swagger funcionan.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Indica que la API está levantada.
    /// </summary>
    /// <response code="200">La API responde correctamente.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new { estado = "ok" });
    }
}
