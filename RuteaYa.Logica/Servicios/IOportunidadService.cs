using RuteaYa.Logica.Dtos.Oportunidades;

namespace RuteaYa.Logica.Servicios;

public interface IOportunidadService
{
    /// <summary>Envíos disponibles que el conductor puede sumar a su viaje (HU-11).</summary>
    Task<IReadOnlyList<OportunidadResponse>> BuscarAsync(
        int viajeId,
        int usuarioId,
        OrdenOportunidades orden,
        CancellationToken cancellationToken);

    /// <summary>Métricas de un envío para un viaje y si todavía se puede aceptar (HU-12).</summary>
    Task<OportunidadDetalleResponse> ObtenerDetalleAsync(
        int viajeId,
        int envioId,
        int usuarioId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Vuelve a evaluar la compatibilidad justo antes de aceptar. Lanza ConflictException (409)
    /// con los motivos si el envío dejó de ser compatible. Lo usa la aceptación (HU-16).
    /// </summary>
    Task VerificarCompatibilidadAsync(
        int viajeId,
        int envioId,
        int usuarioId,
        CancellationToken cancellationToken);
}
