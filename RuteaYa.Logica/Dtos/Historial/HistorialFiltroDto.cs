using RuteaYa.Entidades.Enums;

namespace RuteaYa.Logica.Dtos.Historial;

public sealed class HistorialFiltroDto
{
    public DateOnly? Desde { get; init; }
    public DateOnly? Hasta { get; init; }
    public EstadoEnvio? Estado { get; init; }
    public int Pagina { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
