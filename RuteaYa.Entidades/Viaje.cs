using RuteaYa.Entidades.Enums;

namespace RuteaYa.Entidades;

public class Viaje
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public DiasSemana DiasSemana { get; set; }
    public TimeOnly HoraSalida { get; set; }
    public TimeOnly HoraLlegada { get; set; }

    public int MaxPaquetes { get; set; }
    public decimal PesoMaximoKg { get; set; }
    public decimal DesvioMaximoKm { get; set; }
    public EstadoViaje Estado { get; set; } = EstadoViaje.Activo;

    public ICollection<Parada> Paradas { get; set; } = new List<Parada>();
    public ICollection<Asignacion> Asignaciones { get; set; } = new List<Asignacion>();
}
