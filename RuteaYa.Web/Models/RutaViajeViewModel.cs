namespace RuteaYa.Web.Models;

/// <summary>Ruta calculada por la API para el alta, la edición y el detalle del viaje.</summary>
public sealed class RutaViajeViewModel
{
    public decimal DistanciaKm { get; set; }
    public int DuracionMinutos { get; set; }

    /// <summary>Origen, paradas y destino en el orden en que se recorren.</summary>
    public List<string> Puntos { get; set; } = [];
}
