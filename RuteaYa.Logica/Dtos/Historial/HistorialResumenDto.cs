namespace RuteaYa.Logica.Dtos.Historial;

public sealed class HistorialResumenDto
{
    public int ViajesRealizados { get; init; }
    public int EnviosTransportados { get; init; }
    public decimal GananciaEfectiva { get; init; }
    public IReadOnlyCollection<HistorialOperacionDto> Operaciones { get; init; } = [];
    public int PaginaActual { get; init; }
    public int PageSize { get; init; }
    public int TotalRegistros { get; init; }
    public int TotalPaginas { get; init; }
}
