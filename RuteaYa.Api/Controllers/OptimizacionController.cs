using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Logica.Dtos.Optimizacion;
using RuteaYa.Logica.Servicios;

namespace RuteaYa.Api.Controllers;

/// <summary>
/// Optimizador de oportunidades (HU-13).
/// Controller fino: delega en IOptimizacionService. Los errores los convierte ManejadorGlobalDeExcepciones.
/// </summary>
[ApiController]
[Authorize]
[Route("api/optimizacion")]
public sealed class OptimizacionController(IOptimizacionService optimizacionService) : ControllerBase
{
    /// <summary>
    /// Calcula la combinación de envíos que maximiza la ganancia sin superar los límites del viaje.
    /// </summary>
    /// <remarks>
    /// Los candidatos tienen que venir ya filtrados por compatibilidad con el viaje.
    /// Ante empate de ganancia se prefiere la combinación de menor desvío.
    /// También devuelve las mejores alternativas y las combinaciones descartadas con su motivo.
    /// </remarks>
    /// <param name="request">Límites del viaje y envíos candidatos.</param>
    /// <response code="200">Resultado del optimizador. MejorCombinacion es null si ningún envío entra.</response>
    /// <response code="400">Datos inválidos (límites no positivos, envíos repetidos, pago o peso no positivos).</response>
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
