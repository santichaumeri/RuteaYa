using RuteaYa.Entidades.Enums;

namespace RuteaYa.Logica.Dtos.Historial;

public sealed class HistorialOperacionDto
{
    public int ViajeId { get; init; }
    public string Ruta { get; init; } = string.Empty;
    public DateOnly Periodo { get; init; }
    public EstadoEnvio Estado { get; init; }
    public int CantidadPaquetes { get; init; }
    public decimal Monto { get; init; }
}
