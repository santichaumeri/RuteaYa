namespace RuteaYa.Web.Models.Dashboard;

public sealed class EnvioResumenViewModel
{
    public int Id { get; set; }
    public string Ruta { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal MontoOfrecido { get; set; }
    public string Estado { get; set; } = string.Empty;
}
