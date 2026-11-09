using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Logica.Dtos.Asignaciones;
using RuteaYa.Logica.Servicios;
using System.Security.Claims;

namespace RuteaYa.Api.Controllers;

[ApiController]
[Route("api/asignaciones")]
[Authorize]
public class AsignacionController : ControllerBase
{
    private readonly IAsignacionService _asignacionService;

    public AsignacionController(IAsignacionService asignacionService)
    {
        _asignacionService = asignacionService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AsignacionDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AceptarOportunidad([FromBody] AsignarOportunidadRequestDto dto)
    {
        var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(usuarioIdClaim) || !int.TryParse(usuarioIdClaim, out var usuarioId))
            return Unauthorized();

        try
        {
            var resultado = await _asignacionService.AceptarOportunidadAsync(usuarioId, dto);
            return StatusCode(StatusCodes.Status201Created, resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Conflicto de negocio o concurrencia
            return Conflict(new { mensaje = ex.Message });
        }
    }
}

