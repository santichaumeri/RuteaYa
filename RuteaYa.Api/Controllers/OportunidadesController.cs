using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Logica.Dtos.Oportunidades;
using RuteaYa.Logica.Servicios;

namespace RuteaYa.Api.Controllers;

/// <summary>
/// Oportunidades de un viaje: envíos que el conductor puede sumar a su recorrido (HU-11 y HU-12).
/// Controller fino: delega en IOportunidadService. Los errores los convierte ManejadorGlobalDeExcepciones.
/// </summary>
[ApiController]
[Authorize]
[Route("api/viajes/{viajeId:int}/oportunidades")]
public sealed class OportunidadesController(IOportunidadService oportunidadService) : ControllerBase
{
    /// <summary>
    /// Busca los envíos compatibles con un viaje del usuario autenticado.
    /// </summary>
    /// <remarks>
    /// Solo incluye envíos en estado "Buscando conductor", de otros usuarios, con fecha futura en un día en que sale el viaje,
    /// que entran en la capacidad, el peso y el desvío que le quedan al viaje ese día.
    /// </remarks>
    /// <param name="viajeId">Viaje del conductor.</param>
    /// <param name="orden">Ganancia (por defecto), Desvio o Compatibilidad.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="200">Oportunidades ordenadas según el criterio pedido. Puede ser una lista vacía.</response>
    /// <response code="401">Sin token o token inválido.</response>
    /// <response code="403">El viaje pertenece a otro usuario.</response>
    /// <response code="404">El viaje no existe.</response>
    /// <response code="409">El viaje está inactivo.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OportunidadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyList<OportunidadResponse>>> Buscar(
        int viajeId,
        [FromQuery] OrdenOportunidades orden = OrdenOportunidades.Ganancia,
        CancellationToken cancellationToken = default)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        return Ok(await oportunidadService.BuscarAsync(viajeId, usuarioId, orden, cancellationToken));
    }

    /// <summary>
    /// Compara la ruta habitual del viaje con la ruta llevando el envío y dice si todavía se puede aceptar.
    /// </summary>
    /// <remarks>
    /// Usa el mismo cálculo que la búsqueda. Si alguna restricción dejó de cumplirse,
    /// PuedeAceptarse es false y MotivosBloqueo explica por qué.
    /// </remarks>
    /// <param name="viajeId">Viaje del conductor.</param>
    /// <param name="envioId">Envío a evaluar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="200">Métricas de la oportunidad.</response>
    /// <response code="401">Sin token o token inválido.</response>
    /// <response code="403">El viaje pertenece a otro usuario.</response>
    /// <response code="404">El viaje o el envío no existen.</response>
    [HttpGet("{envioId:int}")]
    [ProducesResponseType(typeof(OportunidadDetalleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OportunidadDetalleResponse>> ObtenerDetalle(
        int viajeId,
        int envioId,
        CancellationToken cancellationToken)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        return Ok(await oportunidadService.ObtenerDetalleAsync(viajeId, envioId, usuarioId, cancellationToken));
    }

    private bool TryObtenerUsuarioId(out int usuarioId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioId);
}
