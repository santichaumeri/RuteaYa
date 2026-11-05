using RuteaYa.Logica.Dtos;

namespace RuteaYa.Logica.Servicios;

public interface IViajeService
{
    Task<ViajeResponse> CrearViajeAsync(
        int usuarioId,
        CrearViajeRequest request,
        CancellationToken cancellationToken);
}
