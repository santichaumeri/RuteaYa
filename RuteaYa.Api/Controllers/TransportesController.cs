using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Logica.Dtos.Transportes;
using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Servicios;
using System.Security.Claims;

namespace RuteaYa.Api.Controllers;

/// <summary>Paquetes que el conductor aceptó llevar y su avance (HU-18). Los errores los traduce ManejadorGlobalDeExcepciones.</summary>
[ApiController]
[Route("api/transportes")]
[Authorize]
public class TransportesController : ControllerBase
{
    private readonly ITransporteService _transporteService;

    public TransportesController(ITransporteService transporteService)
    {
        _transporteService = transporteService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TransporteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerMisTransportes() =>
        Ok(await _transporteService.ObtenerMisTransportesAsync(ObtenerUsuarioId()));

    [HttpGet("{asignacionId:int}")]
    [ProducesResponseType(typeof(TransporteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerDetalle(int asignacionId) =>
        Ok(await _transporteService.ObtenerDetalleAsync(asignacionId, ObtenerUsuarioId()));

    [HttpPost("{asignacionId:int}/retirar")]
    [ProducesResponseType(typeof(TransporteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarcarRetirado(int asignacionId) =>
        Ok(await _transporteService.MarcarRetiradoAsync(asignacionId, ObtenerUsuarioId()));

    [HttpPost("{asignacionId:int}/en-camino")]
    [ProducesResponseType(typeof(TransporteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarcarEnCamino(int asignacionId) =>
        Ok(await _transporteService.MarcarEnCaminoAsync(asignacionId, ObtenerUsuarioId()));

    [HttpPost("{asignacionId:int}/entregar")]
    [ProducesResponseType(typeof(TransporteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarcarEntregado(int asignacionId) =>
        Ok(await _transporteService.MarcarEntregadoAsync(asignacionId, ObtenerUsuarioId()));

    private int ObtenerUsuarioId()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            throw new NoAutenticadoException("No se pudo identificar al usuario desde el token provisto.");
        return usuarioId;
    }
}
