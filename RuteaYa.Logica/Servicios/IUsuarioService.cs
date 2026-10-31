using RuteaYa.Logica.Dtos;

namespace RuteaYa.Logica.Servicios;

/// <summary>Casos de uso de usuarios. Lo consume UsuariosController en la API.</summary>
public interface IUsuarioService
{
    /// <summary>Crea un usuario nuevo. Lanza ConflictException si el email ya existe.</summary>
    Task<UsuarioResponse> RegistrarAsync(RegistroRequest request, CancellationToken cancellationToken);

    /// <summary>Valida las credenciales. Lanza NoAutenticadoException si son incorrectas.</summary>
    Task<UsuarioResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
