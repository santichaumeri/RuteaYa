namespace RuteaYa.Logica.Dtos.Optimizacion;

public sealed class OptimizacionResponse
{
    /// <summary>Combinación recomendada. Null si ningún envío entra en el viaje.</summary>
    public CombinacionResponse? MejorCombinacion { get; init; }

    /// <summary>Otras combinaciones válidas, de mejor a peor.</summary>
    public IReadOnlyList<CombinacionResponse> Alternativas { get; init; } = [];

    /// <summary>Combinaciones que superan algún límite, con su motivo.</summary>
    public IReadOnlyList<CombinacionResponse> Descartadas { get; init; } = [];

    public int CombinacionesEvaluadas { get; init; }
}

public sealed class CombinacionResponse
{
    public IReadOnlyList<int> EnvioIds { get; init; } = [];
    public int CantidadPaquetes { get; init; }
    public decimal GananciaTotal { get; init; }
    public decimal PesoTotalKg { get; init; }
    public decimal DesvioTotalKm { get; init; }
    public IReadOnlyList<string> MotivosDescarte { get; init; } = [];
}
