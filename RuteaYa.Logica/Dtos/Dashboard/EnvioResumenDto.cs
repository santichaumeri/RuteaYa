namespace RuteaYa.Logica.Dtos.Dashboard;

public sealed class EnvioResumenDto
{
    public int Id { get; init; }
    public string Ruta { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public decimal MontoOfrecido { get; init; }
    public string Estado { get; init; } = string.Empty;
}
