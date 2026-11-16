namespace RuteaYa.Logica.Dtos;

/// <summary>Ruta habitual del viaje calculada por el servicio de rutas (pantallas 4 y 5).</summary>
public sealed class RutaViajeResponse
{
    public decimal DistanciaKm { get; init; }
    public int DuracionMinutos { get; init; }

    /// <summary>Origen, paradas y destino en el orden en que se recorren.</summary>
    public IReadOnlyList<string> Puntos { get; init; } = [];
}
