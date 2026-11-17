using RuteaYa.Logica.Dtos.Historial;

namespace RuteaYa.Logica.Servicios;

public interface IHistorialService
{
    Task<HistorialResumenDto> ObtenerHistorialAsync(
        int usuarioId,
        HistorialFiltroDto filtro,
        CancellationToken cancellationToken = default);

    Task<HistorialDetalleDto> ObtenerDetalleAsync(
        int usuarioId,
        int viajeId,
        CancellationToken cancellationToken = default);
}
