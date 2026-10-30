using Microsoft.AspNetCore.Mvc;
using RuteaYa.Logica.Dtos;
using RuteaYa.Logica.Servicios;

namespace RuteaYa.Api.Controllers;

/// <summary>
/// Registro e inicio de sesión de usuarios.
/// Controller fino: delega todo en IUsuarioService. Los errores de negocio los convierte
/// ManejadorGlobalDeExcepciones, por eso no hay try/catch acá.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    /// <summary>
    /// Registra un usuario nuevo.
    /// </summary>
    /// <param name="request">Nombre, apellido, email, contraseña y su confirmación.</param>
    /// <param name="cancellationToken">Cancela la operación si el cliente corta la conexión.</param>
    /// <response code="201">Usuario creado.</response>
    /// <response code="400">Datos inválidos (campo vacío, email mal formado, contraseña corta o que no coincide).</response>
    /// <response code="409">Ya existe un usuario con ese email.</response>
    [HttpPost("registro")]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioResponse>> Registro(
        RegistroRequest request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioService.RegistrarAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, usuario);
    }

    /// <summary>
    /// Valida las credenciales y devuelve los datos del usuario.
    /// La sesión (cookie) la crea el sitio MVC, no la API.
    /// </summary>
    /// <param name="request">Email y contraseña.</param>
    /// <param name="cancellationToken">Cancela la operación si el cliente corta la conexión.</param>
    /// <response code="200">Credenciales correctas.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="401">Credenciales inválidas.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UsuarioResponse>> Login(
        LoginRequest request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioService.LoginAsync(request, cancellationToken);
        return Ok(usuario);
    }
}
