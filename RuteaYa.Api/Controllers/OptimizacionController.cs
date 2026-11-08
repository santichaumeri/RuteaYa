using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Logica.Dtos.Optimizacion;
using RuteaYa.Logica.Servicios;

namespace RuteaYa.Api.Controllers;

/// <summary>
/// Optimizador de oportunidades (HU-13) con criterios de optimización (HU-15).
/// Controller fino: delega en IOptimizacionService. Los errores los convierte ManejadorGlobalDeExcepciones.
/// </summary>
[ApiController]
[Authorize]
[Route("api/optimizacion")]
public sealed class OptimizacionController(IOptimizacionService optimizacionService) : ControllerBase
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
}
