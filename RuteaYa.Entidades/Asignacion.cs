namespace RuteaYa.Entidades;

/// <summary>Un envío aceptado por un conductor para un viaje (el "transporte").</summary>
public class Asignacion
{
    public int Id { get; set; }

    public int ViajeId { get; set; }
    public Viaje Viaje { get; set; } = null!;

    public int EnvioId { get; set; }
    public Envio Envio { get; set; } = null!;

    public DateTime FechaAsignacion { get; set; }
    public decimal DesvioKm { get; set; }
    public decimal Ganancia { get; set; }
}
