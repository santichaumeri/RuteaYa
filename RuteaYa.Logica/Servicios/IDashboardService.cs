using RuteaYa.Logica.Dtos.Dashboard;

namespace RuteaYa.Logica.Servicios;

public interface IDashboardService
{
    Task<DashboardResumenDto?> ObtenerResumenAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);
}
