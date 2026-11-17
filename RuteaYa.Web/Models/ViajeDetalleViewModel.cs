namespace RuteaYa.Web.Models;

public sealed class ViajeDetalleViewModel
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public int DiasSemana { get; set; }
    public TimeSpan HoraSalida { get; set; }
    public TimeSpan HoraLlegada { get; set; }
    public int MaxPaquetes { get; set; }
    public decimal PesoMaximoKg { get; set; }
    public decimal DesvioMaximoKm { get; set; }
    public int Estado { get; set; }
    public int CantidadPaquetesAsignados { get; set; }
    public List<PaqueteAsignadoViewModel> PaquetesAsignados { get; set; } = [];
    public IReadOnlyCollection<ParadaDetalleViewModel> Paradas { get; set; } = [];
    public RutaViajeViewModel? Ruta { get; set; }
}

public sealed class ParadaDetalleViewModel
{
    public int Id { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public int Orden { get; set; }
}

/// <summary>Paquete que el conductor aceptó llevar en el viaje.</summary>
public sealed class PaqueteAsignadoViewModel
{
    public int AsignacionId { get; set; }
    public int EnvioId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public decimal PesoKg { get; set; }
    public EstadoEnvio Estado { get; set; }
    public decimal Ganancia { get; set; }
}
