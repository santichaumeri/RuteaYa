using RuteaYa.Logica.Dtos;
using RuteaYa.Entidades.Enums;

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

    Task<IReadOnlyCollection<ViajeResponse>> ObtenerMisViajesAsync(
        int usuarioId,
        EstadoViaje? estado,
        CancellationToken cancellationToken);

    Task DesactivarViajeAsync(
        int usuarioId,
        int viajeId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Calcula la ruta de un recorrido sin guardarlo, para el botón "Calcular ruta" del formulario.
    /// Valida origen, destino y paradas con las mismas reglas que el alta.
    /// </summary>
    Task<RutaViajeResponse> CalcularRutaAsync(
        RutaViajeRequest request,
        CancellationToken cancellationToken);
}
