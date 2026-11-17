using RuteaYa.Entidades.Enums;

namespace RuteaYa.Logica.Dtos;

public class ViajeDetalleResponse
{
    public int Id { get; init; }
    public int UsuarioId { get; init; }
    public string Origen { get; init; } = string.Empty;
    public string Destino { get; init; } = string.Empty;
    public DiasSemana DiasSemana { get; init; }
    public TimeSpan HoraSalida { get; init; }
    public TimeSpan HoraLlegada { get; init; }
    public int MaxPaquetes { get; init; }
    public decimal PesoMaximoKg { get; init; }
    public decimal DesvioMaximoKm { get; init; }
    public EstadoViaje Estado { get; init; }
    public int CantidadPaquetesAsignados { get; init; }

    /// <summary>Paquetes que el conductor aceptó llevar en este viaje, del más reciente al más antiguo.</summary>
    public IReadOnlyList<PaqueteAsignadoResponse> PaquetesAsignados { get; init; } = [];
    public IReadOnlyCollection<ParadaResponse> Paradas { get; init; } = [];
    public RutaViajeResponse? Ruta { get; init; }
}

/// <summary>Un paquete asignado al viaje, para el detalle del viaje (pantalla 5).</summary>
public class PaqueteAsignadoResponse
{
    /// <summary>Id para consultar o actualizar el transporte en api/transportes.</summary>
    public int AsignacionId { get; init; }
    public int EnvioId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public string Origen { get; init; } = string.Empty;
    public string Destino { get; init; } = string.Empty;
    public DateOnly Fecha { get; init; }
    public decimal PesoKg { get; init; }
    public EstadoEnvio Estado { get; init; }
    public decimal Ganancia { get; init; }
}
