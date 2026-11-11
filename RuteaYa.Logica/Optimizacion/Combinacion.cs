namespace RuteaYa.Logica.Optimizacion;

/// <summary>Un conjunto de envíos evaluado por el optimizador, con sus totales.</summary>
public sealed class Combinacion
{
    public IReadOnlyList<CandidatoEnvio> Envios { get; init; } = [];
    public decimal GananciaTotal { get; init; }
    public decimal PesoTotalKg { get; init; }
    public decimal DesvioTotalKm { get; init; }

    /// <summary>Por qué la combinación no cumple las restricciones. Vacío si es válida.</summary>
    public IReadOnlyList<string> MotivosDescarte { get; init; } = [];

    public int CantidadPaquetes => Envios.Count;
    public bool EsValida => MotivosDescarte.Count == 0;
    public IEnumerable<int> EnvioIds => Envios.Select(e => e.EnvioId);
}
