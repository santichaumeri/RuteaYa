namespace RuteaYa.Web.Models.Dashboard;

public sealed class ViajeResumenViewModel
{
    public int Id { get; set; }
    public string Ruta { get; set; } = string.Empty;
    public string Dias { get; set; } = string.Empty;
    public string Horario { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}
