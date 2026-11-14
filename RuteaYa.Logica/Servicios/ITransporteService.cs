using RuteaYa.Logica.Dtos.Transportes;

namespace RuteaYa.Logica.Servicios;

public interface ITransporteService
{
    /// <summary>Paquetes que el conductor aceptó llevar, en cualquiera de sus viajes.</summary>
    Task<IReadOnlyList<TransporteDto>> ObtenerMisTransportesAsync(int conductorId);

    /// <summary>Detalle de un transporte. NotFound si no existe, AccesoDenegado si no es del conductor.</summary>
    Task<TransporteDto> ObtenerDetalleAsync(int asignacionId, int usuarioId);

    /// <summary>ConductorAsignado → Retirado.</summary>
    Task<TransporteDto> MarcarRetiradoAsync(int asignacionId, int usuarioId);

    /// <summary>Retirado → EnCamino.</summary>
    Task<TransporteDto> MarcarEnCaminoAsync(int asignacionId, int usuarioId);

    /// <summary>EnCamino → Entregado. Lanza TransicionEstadoInvalidaException (409) si no pasó por EnCamino.</summary>
    Task<TransporteDto> MarcarEntregadoAsync(int asignacionId, int usuarioId);
}
