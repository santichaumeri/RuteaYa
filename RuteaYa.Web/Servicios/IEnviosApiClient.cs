using RuteaYa.Web.Models;
using RuteaYa.Web.Models.Envios;

namespace RuteaYa.Web.Servicios;

/// <summary>
/// Envíos del usuario a través de la API. Si la API responde con error, lanza <see cref="ApiException"/>
/// con el código HTTP y el mensaje que devolvió, así la pantalla puede mostrar el motivo real.
/// </summary>
public interface IEnviosApiClient
{
    Task<IReadOnlyList<EnvioViewModel>> ObtenerMisEnviosAsync(EstadoEnvio? estado, CancellationToken cancellationToken);
    Task<EnvioViewModel> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);
    Task<EnvioViewModel> CrearEnvioAsync(CrearEnvioViewModel modelo, CancellationToken cancellationToken);
    Task CancelarEnvioAsync(int id, CancellationToken cancellationToken);
}
