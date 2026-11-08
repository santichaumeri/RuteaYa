using RuteaYa.Logica.Dtos.Oportunidades;

namespace RuteaYa.Logica.Dtos.Optimizacion;

/// <summary>
/// Optimizador aplicado a un viaje en una fecha (pantalla 10, HU-14).
/// Las restricciones son lo que le queda al viaje ese día y los candidatos, sus oportunidades compatibles.
/// </summary>
public sealed class OptimizacionViajeResponse
{
    public int ViajeId { get; init; }
    public string ViajeOrigen { get; init; } = string.Empty;
    public string ViajeDestino { get; init; } = string.Empty;

    public int MaxPaquetes { get; init; }
    public decimal PesoMaximoKg { get; init; }
    public decimal DesvioMaximoKm { get; init; }

    /// <summary>Fechas futuras del viaje que tienen al menos un envío compatible.</summary>
    public IReadOnlyList<DateOnly> FechasConOportunidades { get; init; } = [];

    /// <summary>Fecha optimizada. Null si el viaje no tiene oportunidades en ninguna fecha.</summary>
    public DateOnly? Fecha { get; init; }

    /// <summary>Lo que el viaje ya tiene comprometido esa fecha por envíos aceptados antes.</summary>
    public int PaquetesAsignados { get; init; }
    public decimal PesoAsignadoKg { get; init; }
    public decimal DesvioAsignadoKm { get; init; }

    /// <summary>Envíos compatibles de esa fecha (para mostrar origen, destino y demás datos de cada id).</summary>
    public IReadOnlyList<OportunidadResponse> Candidatos { get; init; } = [];

    /// <summary>Resultado del optimizador. Null si no hay candidatos esa fecha.</summary>
    public OptimizacionResponse? Resultado { get; init; }
}
