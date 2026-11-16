namespace RuteaYa.Logica.Dtos;

/// <summary>Recorrido a calcular desde el formulario del viaje, antes de guardarlo (pantalla 4).</summary>
public sealed class RutaViajeRequest
{
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;

    /// <summary>Paradas intermedias en el orden en que se recorren.</summary>
    public List<string> Paradas { get; set; } = [];
}
