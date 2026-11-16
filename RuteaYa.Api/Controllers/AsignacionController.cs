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

    /// <summary>
    /// Acepta una oportunidad: asigna un envío a un viaje del usuario (HU-16).
    /// </summary>
    /// <remarks>
    /// Usa las mismas reglas que GET api/viajes/{viajeId}/oportunidades/{envioId}: el viaje tiene que salir
    /// el día del envío y le tienen que quedar lugar, peso y desvío <b>en esa fecha</b>.
    /// Los errores los convierte ManejadorGlobalDeExcepciones.
    /// </remarks>
    /// <response code="201">Envío asignado al viaje.</response>
    /// <response code="401">Sin token o token inválido.</response>
    /// <response code="403">El viaje pertenece a otro usuario.</response>
    /// <response code="404">El viaje o el envío no existen.</response>
    /// <response code="409">El envío ya no es compatible (motivos en el detalle) o lo tomó otro conductor.</response>
    [HttpPost]
    [ProducesResponseType(typeof(AsignacionDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AsignacionDetalleDto>> AceptarOportunidad(
        [FromBody] AsignarOportunidadRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized();

        var resultado = await _asignacionService.AceptarOportunidadAsync(usuarioId, dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, resultado);
    }

    
    [HttpPost("combinacion")]
    [ProducesResponseType(typeof(CombinacionConfirmadaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CombinacionConfirmadaDto>> ConfirmarCombinacion(
        [FromBody] ConfirmarCombinacionRequestDto dto)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized();

        var resultado = await _asignacionService.ConfirmarCombinacionAsync(usuarioId, dto);
        return StatusCode(StatusCodes.Status201Created, resultado);
    }
}

