using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Logica.Dtos.Optimizacion;
using RuteaYa.Logica.Optimizacion.Criterios;
using RuteaYa.Logica.Servicios;

namespace RuteaYa.Api.Controllers;

/// <summary>
/// Optimizador de oportunidades (HU-13) con criterios de optimización (HU-15).
/// Controller fino: delega en IOptimizacionService. Los errores los convierte ManejadorGlobalDeExcepciones.
/// </summary>
[ApiController]
[Authorize]
[Route("api/optimizacion")]
public sealed class OptimizacionController(
    IOptimizacionService optimizacionService,
    IOptimizacionViajeService optimizacionViajeService) : ControllerBase
{
    /// <summary>
    /// Calcula la mejor combinación de envíos según el criterio elegido sin superar los límites del viaje.
    /// </summary>
    /// <remarks>
    /// Los candidatos tienen que venir ya filtrados por compatibilidad con el viaje.
    ///
    /// Criterios (campo `criterio`, por defecto `MaximizarGanancias`):
    /// - **MaximizarGanancias**: mayor ganancia; ante empate, menor desvío (regla del enunciado).
    /// - **MinimizarDesvios**: menor desvío total; ante empate, mayor ganancia.
    /// - **Equilibrio**: mayor ganancia neta = ganancia − costo por km × desvío (costo configurable en
    ///   `Optimizacion:Equilibrio:CostoPorKm`, por defecto $1000); ante empate, menor desvío.
    ///
    /// También devuelve las mejores alternativas según el mismo criterio y las combinaciones descartadas con su motivo.
    /// </remarks>
    /// <param name="request">Límites del viaje y envíos candidatos.</param>
    /// <response code="200">Resultado del optimizador. MejorCombinacion es null si ningún envío entra.</response>
    /// <response code="400">Datos inválidos (límites no positivos, envíos repetidos, pago o peso no positivos, criterio inexistente).</response>
    /// <response code="401">Sin token o token inválido.</response>
    [HttpPost]
    [ProducesResponseType(typeof(OptimizacionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<OptimizacionResponse> Calcular(OptimizarRequest request)
    {
        return Ok(optimizacionService.Calcular(request));
    }

    /// <summary>
    /// Calcula la mejor combinación de oportunidades para un viaje del usuario en una fecha (pantalla Optimizador).
    /// </summary>
    /// <remarks>
    /// Toma como candidatos los envíos compatibles con el viaje esa fecha (los mismos de
    /// GET api/viajes/{viajeId}/oportunidades) y como límites lo que le queda al viaje ese día,
    /// descontando los envíos ya asignados. Cada fecha se optimiza por separado porque la capacidad es diaria.
    ///
    /// Si no se indica fecha, usa la más próxima con oportunidades. Resultado es null cuando esa fecha no tiene candidatos.
    /// </remarks>
    /// <param name="viajeId">Viaje del conductor.</param>
    /// <param name="fecha">Fecha a optimizar (yyyy-MM-dd). Opcional.</param>
    /// <param name="criterio">MaximizarGanancias (por defecto), MinimizarDesvios o Equilibrio.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="200">Uso del viaje esa fecha, candidatos y combinación recomendada.</response>
    /// <response code="400">Criterio inválido.</response>
    /// <response code="401">Sin token o token inválido.</response>
    /// <response code="403">El viaje pertenece a otro usuario.</response>
    /// <response code="404">El viaje no existe.</response>
    /// <response code="409">El viaje está inactivo.</response>
    [HttpGet("/api/viajes/{viajeId:int}/optimizacion")]
    [ProducesResponseType(typeof(OptimizacionViajeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OptimizacionViajeResponse>> CalcularParaViaje(
        int viajeId,
        [FromQuery] DateOnly? fecha,
        [FromQuery] CriterioOptimizacion criterio = CriterioOptimizacion.MaximizarGanancias,
        CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
        {
            return Unauthorized();
        }

        return Ok(await optimizacionViajeService.CalcularAsync(viajeId, usuarioId, fecha, criterio, cancellationToken));
    }
}
