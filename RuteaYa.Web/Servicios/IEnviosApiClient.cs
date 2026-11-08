using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Dtos.Envios;

namespace RuteaYa.Web.Servicios;

public interface IEnviosApiClient
{
    Task<List<EnvioDetalleDto>> ObtenerMisEnviosAsync(EstadoEnvio? estado = null);
    Task<EnvioDetalleDto?> ObtenerPorIdAsync(int id);
    Task<EnvioDetalleDto?> CrearEnvioAsync(CrearEnvioDto dto);
    Task<bool> CancelarEnvioAsync(int id);
}
