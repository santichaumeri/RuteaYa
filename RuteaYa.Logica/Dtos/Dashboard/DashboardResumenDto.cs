namespace RuteaYa.Logica.Dtos.Dashboard;

public sealed class DashboardResumenDto
{
    public string NombreUsuario { get; init; } = string.Empty;
    public int ProximosViajes { get; init; }
    public int EnviosPendientes { get; init; }
    public int Oportunidades { get; init; }
    public decimal PotencialMes { get; init; }
    public IReadOnlyCollection<ViajeResumenDto> Viajes { get; init; } = [];
    public IReadOnlyCollection<EnvioResumenDto> Envios { get; init; } = [];
}
