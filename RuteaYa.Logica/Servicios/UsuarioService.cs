using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos;
using RuteaYa.Logica.Excepciones;

namespace RuteaYa.Logica.Servicios;

/// <summary>
/// Reglas de negocio de registro e inicio de sesión.
/// Habla con la base a través del DbContext y avisa los errores con excepciones de negocio,
/// que ManejadorGlobalDeExcepciones (en la API) convierte en respuestas HTTP.
/// </summary>
public class UsuarioService : IUsuarioService
{
    private const string MensajeCredencialesInvalidas = "Credenciales inválidas.";

    private readonly RuteaYaDbContext _context;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public UsuarioService(RuteaYaDbContext context, IPasswordHasher<Usuario> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<UsuarioResponse> RegistrarAsync(RegistroRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizarEmail(request.Email);

        // Email único: se chequea antes para dar un 409 claro.
        var existe = await _context.Usuarios.AnyAsync(u => u.Email == email, cancellationToken);
        if (existe)
        {
            throw new ConflictException("Ya existe un usuario registrado con ese email.");
        }

        var usuario = new Usuario
        {
            Nombre = request.Nombre.Trim(),
            Apellido = request.Apellido.Trim(),
            Email = email
        };
        // La contraseña nunca se guarda en texto plano: solo el hash.
        usuario.PasswordHash = _passwordHasher.HashPassword(usuario, request.Password);

        _context.Usuarios.Add(usuario);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Dos registros simultáneos con el mismo email: lo frena el índice único de la base.
            throw new ConflictException("Ya existe un usuario registrado con ese email.");
        }

        return ADto(usuario);
    }

    public async Task<UsuarioResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizarEmail(request.Email);

        var usuario = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        // Mismo mensaje para "email inexistente" y "contraseña incorrecta":
        // así no se revela si un email está registrado.
        if (usuario is null)
        {
            throw new NoAutenticadoException(MensajeCredencialesInvalidas);
        }

        var resultado = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password);
        if (resultado == PasswordVerificationResult.Failed)
        {
            throw new NoAutenticadoException(MensajeCredencialesInvalidas);
        }

        return ADto(usuario);
    }

    // Se guarda y se busca siempre en minúsculas y sin espacios para que el chequeo de duplicados sea consistente.
    private static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();

    // Mapeo manual entidad -> DTO (sin AutoMapper).
    private static UsuarioResponse ADto(Usuario usuario) => new()
    {
        Id = usuario.Id,
        Nombre = usuario.Nombre,
        Apellido = usuario.Apellido,
        Email = usuario.Email
    };
}
