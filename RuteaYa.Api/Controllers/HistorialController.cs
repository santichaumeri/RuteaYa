using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Dtos.Historial;
using RuteaYa.Logica.Servicios;

namespace RuteaYa.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/historial")]
public sealed class HistorialController(IHistorialService historialService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(HistorialResumenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<HistorialResumenDto>> Obtener(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] EstadoEnvio? estado,
        [FromQuery] int pagina = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
        {
            return Unauthorized();
        }

        var resultado = await historialService.ObtenerHistorialAsync(
            usuarioId,
            new HistorialFiltroDto
            {
                Desde = desde,
                Hasta = hasta,
                Estado = estado,
                Pagina = pagina,
                PageSize = pageSize
            },
            cancellationToken);

        return Ok(resultado);
    }

    [HttpGet("{viajeId:int}")]
    [ProducesResponseType(typeof(HistorialDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<HistorialDetalleDto>> Detalle(
        int viajeId,
        CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
        {
            return Unauthorized();
        }

        return Ok(await historialService.ObtenerDetalleAsync(usuarioId, viajeId, cancellationToken));
    }
}
