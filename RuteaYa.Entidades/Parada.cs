namespace RuteaYa.Entidades;

public class Parada
{
    public int Id { get; set; }
    public int ViajeId { get; set; }
    public Viaje Viaje { get; set; } = null!;

    public string Direccion { get; set; } = string.Empty;

    /// <summary>Posición de la parada dentro del recorrido (1, 2, 3...).</summary>
    public int Orden { get; set; }
}
