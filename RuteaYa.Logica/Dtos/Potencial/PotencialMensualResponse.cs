using RuteaYa.Logica.Optimizacion.Criterios;

namespace RuteaYa.Logica.Dtos.Potencial;

/// <summary>
/// Estimación de cuánto podría ganar el conductor en un mes con sus viajes recurrentes (HU-20).
/// Es una simulación: no reserva envíos ni crea asignaciones.
/// </summary>
public sealed class PotencialMensualResponse
{
    public int Anio { get; init; }
    public int Mes { get; init; }

    /// <summary>Criterio con el que se eligieron los envíos de cada salida.</summary>
    public CriterioOptimizacion Criterio { get; init; }
    public string CriterioDescripcion { get; init; } = string.Empty;

    /// <summary>Primer día analizado: el 1 del mes, o hoy si el mes ya empezó.</summary>
    public DateOnly Desde { get; init; }

    /// <summary>Último día del mes.</summary>
    public DateOnly Hasta { get; init; }

    /// <summary>Viajes recurrentes activos del usuario que se tuvieron en cuenta.</summary>
    public int ViajesRecurrentes { get; init; }

    /// <summary>Salidas concretas analizadas (cada viaje en cada fecha en que sale).</summary>
    public int ViajesAnalizados { get; init; }

    public int PaquetesPosibles { get; init; }

    /// <summary>Suma de lo que pagarían los envíos elegidos. Es una estimación, no dinero cobrado.</summary>
    public decimal GananciaPotencial { get; init; }

    /// <summary>Ganancia potencial dividida por la cantidad de salidas analizadas.</summary>
    public decimal PromedioPorViaje { get; init; }

    /// <summary>Ganancia potencial por semana (lunes a domingo, recortada al período analizado).</summary>
    public IReadOnlyList<GananciaSemanaResponse> GananciaPorSemana { get; init; } = [];

    /// <summary>La salida que más ganancia aporta. Null si ninguna tiene envíos.</summary>
    public SalidaPotencialResponse? MejorRecorrido { get; init; }

    /// <summary>Qué envíos se eligieron en cada salida, en orden cronológico. Justifica la estimación.</summary>
    public IReadOnlyList<SalidaPotencialResponse> Detalle { get; init; } = [];

    /// <summary>
    /// Ganancia efectiva: lo que ya ganó el conductor ese mes con envíos entregados.
    /// No se suma a la potencial.
    /// </summary>
    public decimal GananciaEfectiva { get; init; }

    /// <summary>Siempre true: el cálculo no reserva envíos.</summary>
    public bool EsSimulacion => true;
}

public sealed class SalidaPotencialResponse
{
    public int ViajeId { get; init; }
    public string Origen { get; init; } = string.Empty;
    public string Destino { get; init; } = string.Empty;
    public DateOnly Fecha { get; init; }
    public IReadOnlyList<int> EnvioIds { get; init; } = [];

    /// <summary>Los envíos elegidos para esta salida, para mostrar de dónde sale la ganancia.</summary>
    public IReadOnlyList<EnvioPotencialResponse> Envios { get; init; } = [];
    public int Paquetes { get; init; }
    public decimal PesoKg { get; init; }
    public decimal DesvioKm { get; init; }
    public decimal Ganancia { get; init; }
}

public sealed class EnvioPotencialResponse
{
    public int EnvioId { get; init; }
    public string Origen { get; init; } = string.Empty;
    public string Destino { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public decimal PesoKg { get; init; }
    public decimal DesvioKm { get; init; }
    public decimal Pago { get; init; }
}

public sealed class GananciaSemanaResponse
{
    public DateOnly Desde { get; init; }
    public DateOnly Hasta { get; init; }
    public int Paquetes { get; init; }
    public decimal Ganancia { get; init; }
}
