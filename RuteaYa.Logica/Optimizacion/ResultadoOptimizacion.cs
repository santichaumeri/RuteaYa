namespace RuteaYa.Logica.Optimizacion;

public sealed class ResultadoOptimizacion
{
    /// <summary>La combinación recomendada. Null si ningún envío entra en el viaje.</summary>
    public Combinacion? MejorCombinacion { get; init; }

    /// <summary>Las siguientes combinaciones válidas, de mejor a peor (sin incluir la recomendada).</summary>
    public IReadOnlyList<Combinacion> Alternativas { get; init; } = [];

    /// <summary>Combinaciones que superan algún límite, con su motivo (las de mayor ganancia primero).</summary>
    public IReadOnlyList<Combinacion> Descartadas { get; init; } = [];

    /// <summary>Cantidad total de combinaciones que se evaluaron (válidas y descartadas).</summary>
    public int CombinacionesEvaluadas { get; init; }
}
