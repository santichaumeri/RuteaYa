using RuteaYa.Logica.Dtos;

namespace RuteaYa.Logica.Servicios;

public interface IViajeService
{
    Task<ViajeResponse> CrearViajeAsync(
        int usuarioId,
        CrearViajeRequest request,
        CancellationToken cancellationToken);

    Task<ViajeDetalleResponse> ObtenerDetalleAsync(
        int viajeId,
        int usuarioId,
        CancellationToken cancellationToken);

    Task<ViajeDetalleResponse> EditarViajeAsync(
        int viajeId,
        int usuarioId,
        EditarViajeRequest request,
        CancellationToken cancellationToken);
}
