
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Dtos.Envios;
using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Servicios;
using System.Security.Claims;

namespace RuteaYa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Exige que la petición tenga un token JWT válido
public class EnviosController : ControllerBase
{
    private readonly IEnvioService _envioService;

    public EnviosController(IEnvioService envioService)
    {
        _envioService = envioService;
    }

    /// <summary>
    /// Publica un nuevo paquete para ser transportado.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(EnvioDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Crear([FromBody] CrearEnvioDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var usuarioId = ObtenerUsuarioIdAutenticado();

        try
        {
            var nuevoEnvio = await _envioService.CrearEnvioAsync(dto, usuarioId);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = nuevoEnvio.Id }, nuevoEnvio);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Lista los envíos del usuario autenticado, con filtro opcional por estado.
    /// </summary>

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EnvioDetalleDto>),StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerMisEnvios([FromQuery] EstadoEnvio? estado)
    {
        var usuarioId = ObtenerUsuarioIdAutenticado();
        var envios = await _envioService.ObtenerMisEnviosAsync(usuarioId, estado);
        return Ok(envios);
    }

    /// <summary>
    /// Obtiene el detalle de un envío específico del usuario autenticado.
    /// </summary>

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EnvioDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]

    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var usuarioId = ObtenerUsuarioIdAutenticado();
        var envio = await _envioService.ObtenerPorIdAsync(id,usuarioId);

        if(envio is null)
        {
            return NotFound(new { mensaje = $"No se encontró el envío con ID {id} para el usuario." });
        }
        return Ok(envio);
    }

    /// <summary>
    /// Cancela un envío del usuario si su estado actual lo permite.
    /// </summary>
    [HttpPost("{id:int}/cancelar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]

    public async Task<IActionResult> Cancelar(int id)
    {
        var usuarioId = ObtenerUsuarioIdAutenticado();

        try
        {
            await _envioService.CancelarEnvioAsync(id, usuarioId);
            return Ok(new { mensaje = "El envío ha sido cancelado exitosamente" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje =ex.Message });
        }
        catch (TransicionEstadoInvalidaException ex)
        {
            return Conflict(new {mensaje =ex.Message});
        }
    }











    /// <summary>
    /// Extrae el identificador del usuario desde los Claims del token de autenticación.
    /// </summary>
    private int ObtenerUsuarioIdAutenticado()
    {
        var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("id")?.Value;

        if(string.IsNullOrEmpty(claimId) || !int.TryParse(claimId, out var usuarioId))
        {
            throw new UnauthorizedAccessException("No se pudo identificar al usuario desde el token provisto.");
        }
        return usuarioId;        
    }
}
