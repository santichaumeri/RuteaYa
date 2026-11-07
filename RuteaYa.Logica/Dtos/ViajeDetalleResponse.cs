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
    public IReadOnlyCollection<ParadaResponse> Paradas { get; init; } = [];
}
