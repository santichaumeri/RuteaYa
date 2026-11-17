using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Logica.Dtos;
using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Servicios;

namespace RuteaYa.Api.Controllers;

/// <summary>
/// Paradas intermedias dentro de un viaje (§9). Solo el dueño del viaje puede verlas o cambiarlas.
/// Los errores de negocio los traduce ManejadorGlobalDeExcepciones (400, 403, 404).
/// </summary>
[ApiController]
[Authorize]
[Route("api/viajes/{viajeId:int}/paradas")]
public sealed class ParadasController(IParadaService paradaService) : ControllerBase
{
    /// <summary>Paradas del viaje en el orden del recorrido.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ParadaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ParadaResponse>>> Obtener(int viajeId, CancellationToken cancellationToken) =>
        Ok(await paradaService.ObtenerAsync(viajeId, ObtenerUsuarioId(), cancellationToken));

    /// <summary>Agrega una parada. Sin "orden" va al final; con "orden" se inserta ahí y corre las siguientes.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ParadaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParadaResponse>> Crear(int viajeId, ParadaRequest request, CancellationToken cancellationToken)
    {
        var parada = await paradaService.CrearAsync(viajeId, ObtenerUsuarioId(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, parada);
    }

    /// <summary>Cambia la dirección de la parada y, si se indica "orden", la mueve a esa posición.</summary>
    [HttpPut("{paradaId:int}")]
    [ProducesResponseType(typeof(ParadaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParadaResponse>> Modificar(
        int viajeId, int paradaId, ParadaRequest request, CancellationToken cancellationToken) =>
        Ok(await paradaService.ModificarAsync(viajeId, paradaId, ObtenerUsuarioId(), request, cancellationToken));

    /// <summary>Elimina la parada y renumera las demás.</summary>
    [HttpDelete("{paradaId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(int viajeId, int paradaId, CancellationToken cancellationToken)
    {
        await paradaService.EliminarAsync(viajeId, paradaId, ObtenerUsuarioId(), cancellationToken);
        return NoContent();
    }

    private int ObtenerUsuarioId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId)
            ? usuarioId
            : throw new NoAutenticadoException("No se pudo identificar al usuario desde el token provisto.");
}
