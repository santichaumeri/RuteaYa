using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Logica.Dtos.Potencial;
using RuteaYa.Logica.Optimizacion.Criterios;
using RuteaYa.Logica.Servicios;

namespace RuteaYa.Api.Controllers;

/// <summary>
/// Potencial de ganancias mensual del conductor (HU-20).
/// Controller fino: delega en IPotencialMensualService. Los errores los convierte ManejadorGlobalDeExcepciones.
/// </summary>
[ApiController]
[Authorize]
[Route("api/potencial")]
public sealed class PotencialController(IPotencialMensualService potencialMensualService) : ControllerBase
{
    /// <summary>
    /// Estima cuánto podría ganar el usuario en un mes con sus viajes recurrentes activos.
    /// </summary>
    /// <remarks>
    /// Expande cada viaje a las fechas del mes en que sale (desde hoy si el mes ya empezó) y,
    /// en orden cronológico, elige los envíos compatibles con el optimizador y el criterio pedido.
    /// Un envío elegido para una salida no se vuelve a contar en otra.
    ///
    /// Es una simulación: no reserva envíos ni crea asignaciones. La ganancia potencial es una estimación;
    /// la ganancia efectiva es lo que ya se cobró ese mes por envíos entregados.
    /// </remarks>
    /// <param name="anio">Año, por ejemplo 2026.</param>
    /// <param name="mes">Mes de 1 a 12.</param>
    /// <param name="criterio">MaximizarGanancias (por defecto), MinimizarDesvios o Equilibrio.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="200">Estimación del mes con el detalle de cada salida.</response>
    /// <response code="400">Mes, año o criterio inválidos, o el mes ya terminó.</response>
    /// <response code="401">Sin token o token inválido.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PotencialMensualResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PotencialMensualResponse>> Calcular(
        [FromQuery] int anio,
        [FromQuery] int mes,
        [FromQuery] CriterioOptimizacion criterio = CriterioOptimizacion.MaximizarGanancias,
        CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
        {
            return Unauthorized();
        }

        return Ok(await potencialMensualService.CalcularAsync(usuarioId, anio, mes, criterio, cancellationToken));
    }
}
