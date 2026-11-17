using RuteaYa.Entidades.Enums;

namespace RuteaYa.Logica.Dtos.Historial;

public sealed class HistorialDetalleDto
{
    public int ViajeId { get; init; }
    public string Ruta { get; init; } = string.Empty;
    public IReadOnlyCollection<HistorialEnvioDto> Envios { get; init; } = [];
    public decimal GananciaEfectiva { get; init; }
}

public sealed class HistorialEnvioDto
{
    public int EnvioId { get; init; }
    public DateOnly Fecha { get; init; }
    public EstadoEnvio Estado { get; init; }
    public decimal Ganancia { get; init; }
}
