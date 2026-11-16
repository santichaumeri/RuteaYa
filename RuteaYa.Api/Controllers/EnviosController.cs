using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Dtos.Envios;
using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Servicios;
using System.Security.Claims;

namespace RuteaYa.Api.Controllers;

/// <summary>
/// Envíos del usuario (HU-07, HU-08). Controller fino: delega en IEnvioService.
/// Los errores los convierte ManejadorGlobalDeExcepciones (400, 401, 404, 409).
/// </summary>
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
    /// <response code="201">Envío publicado en estado "Buscando conductor".</response>
    /// <response code="400">Datos inválidos (campo vacío, origen igual al destino, fecha pasada, peso o monto no positivos).</response>
    /// <response code="401">Sin token o token inválido.</response>
    [HttpPost]
    [ProducesResponseType(typeof(EnvioDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Crear([FromBody] CrearEnvioDto dto)
    {
        // [ApiController] ya respondió 400 si fallaron las DataAnnotations; el servicio valida las reglas de negocio.
        var nuevoEnvio = await _envioService.CrearEnvioAsync(dto, ObtenerUsuarioIdAutenticado());
        return CreatedAtAction(nameof(ObtenerPorId), new { id = nuevoEnvio.Id }, nuevoEnvio);
    }

    /// <summary>
    /// Lista los envíos del usuario autenticado, con filtro opcional por estado.
    /// </summary>
    /// <response code="200">Envíos propios, del más lejano en fecha al más próximo. Puede ser una lista vacía.</response>
    /// <response code="401">Sin token o token inválido.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EnvioDetalleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerMisEnvios([FromQuery] EstadoEnvio? estado)
    {
        var envios = await _envioService.ObtenerMisEnviosAsync(ObtenerUsuarioIdAutenticado(), estado);
        return Ok(envios);
    }

    /// <summary>
    /// Obtiene el detalle de un envío específico del usuario autenticado.
    /// </summary>
    /// <response code="200">Detalle del envío.</response>
    /// <response code="401">Sin token o token inválido.</response>
    /// <response code="404">El envío no existe o es de otro usuario.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EnvioDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var envio = await _envioService.ObtenerPorIdAsync(id, ObtenerUsuarioIdAutenticado())
            ?? throw new NotFoundException($"No se encontró el envío con ID {id} para el usuario.");

        return Ok(envio);
    }

    /// <summary>
    /// Cancela un envío del usuario si su estado actual lo permite.
    /// </summary>
    /// <response code="200">Envío cancelado.</response>
    /// <response code="401">Sin token o token inválido.</response>
    /// <response code="404">El envío no existe o es de otro usuario.</response>
    /// <response code="409">El estado actual no permite cancelar (por ejemplo, ya fue retirado o entregado).</response>
    [HttpPost("{id:int}/cancelar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancelar(int id)
    {
        await _envioService.CancelarEnvioAsync(id, ObtenerUsuarioIdAutenticado());
        return Ok(new { mensaje = "El envío ha sido cancelado exitosamente" });
    }

    /// <summary>
    /// Extrae el identificador del usuario desde los Claims del token de autenticación.
    /// Si no está, lanza NoAutenticadoException, que el manejador global convierte en 401.
    /// </summary>
    private int ObtenerUsuarioIdAutenticado()
    {
        var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(claimId) || !int.TryParse(claimId, out var usuarioId))
        {
            throw new NoAutenticadoException("No se pudo identificar al usuario desde el token provisto.");
        }
        return usuarioId;
    }
}
