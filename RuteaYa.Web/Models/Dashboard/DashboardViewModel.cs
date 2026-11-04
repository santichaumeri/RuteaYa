namespace RuteaYa.Web.Models.Dashboard;

public sealed class DashboardViewModel
{
    public string NombreUsuario { get; set; } = string.Empty;
    public int ProximosViajes { get; set; }
    public int EnviosPendientes { get; set; }
    public int Oportunidades { get; set; }
    public decimal PotencialMes { get; set; }
    public IReadOnlyCollection<ViajeResumenViewModel> Viajes { get; set; } = [];
    public IReadOnlyCollection<EnvioResumenViewModel> Envios { get; set; } = [];
}
