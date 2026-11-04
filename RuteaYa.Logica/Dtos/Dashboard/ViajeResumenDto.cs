namespace RuteaYa.Logica.Dtos.Dashboard;

public sealed class ViajeResumenDto
{
    public int Id { get; init; }
    public string Ruta { get; init; } = string.Empty;
    public string Dias { get; init; } = string.Empty;
    public string Horario { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
}
