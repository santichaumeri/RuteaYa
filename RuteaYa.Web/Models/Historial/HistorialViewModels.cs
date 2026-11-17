
namespace RuteaYa.Web.Models.Historial;

public sealed class HistorialFiltroViewModel
{
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public EstadoEnvio? Estado { get; set; }
    public int Pagina { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public sealed class HistorialResumenViewModel
{
    public int ViajesRealizados { get; set; }
    public int EnviosTransportados { get; set; }
    public decimal GananciaEfectiva { get; set; }
    public IReadOnlyCollection<HistorialOperacionViewModel> Operaciones { get; set; } = [];
    public int PaginaActual { get; set; }
    public int PageSize { get; set; }
    public int TotalRegistros { get; set; }
    public int TotalPaginas { get; set; }
}

public sealed class HistorialOperacionViewModel
{
    public int ViajeId { get; set; }
    public string Ruta { get; set; } = string.Empty;
    public DateOnly Periodo { get; set; }
    public EstadoEnvio Estado { get; set; }
    public int CantidadPaquetes { get; set; }
    public decimal Monto { get; set; }
}

public sealed class HistorialDetalleViewModel
{
    public int ViajeId { get; set; }
    public string Ruta { get; set; } = string.Empty;
    public IReadOnlyCollection<HistorialEnvioViewModel> Envios { get; set; } = [];
    public decimal GananciaEfectiva { get; set; }
}

public sealed class HistorialEnvioViewModel
{
    public int EnvioId { get; set; }
    public DateOnly Fecha { get; set; }
    public EstadoEnvio Estado { get; set; }
    public decimal Ganancia { get; set; }
}
