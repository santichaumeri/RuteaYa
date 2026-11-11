using RuteaYa.Logica.Optimizacion.Criterios;

namespace RuteaYa.Logica.Dtos.Optimizacion;

public sealed class OptimizacionResponse
{
    /// <summary>Criterio con el que se eligió la combinación.</summary>
    public CriterioOptimizacion Criterio { get; init; }

    /// <summary>Explicación del criterio para mostrar en pantalla.</summary>
    public string CriterioDescripcion { get; init; } = string.Empty;

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

    /// <summary>Ganancia total menos el costo del desvío. Solo se informa con el criterio Equilibrio.</summary>
    public decimal? GananciaNeta { get; init; }
    public IReadOnlyList<string> MotivosDescarte { get; init; } = [];
}
