using RuteaYa.Logica.Dtos;

namespace RuteaYa.Api.Services.Auth;

public interface IJwtTokenService
{
    string CrearToken(UsuarioResponse usuario);
}
