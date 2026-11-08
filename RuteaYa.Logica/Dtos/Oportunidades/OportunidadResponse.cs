using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Oportunidades;

namespace RuteaYa.Logica.Dtos.Oportunidades;

/// <summary>Criterio de orden del listado de oportunidades.</summary>
public enum OrdenOportunidades
{
    /// <summary>Mayor pago primero; ante empate, menor desvío.</summary>
    Ganancia = 1,

    /// <summary>Menor desvío primero; ante empate, mayor pago.</summary>
    Desvio = 2,

    /// <summary>Mejor nivel de compatibilidad primero; ante empate, mayor ganancia por km.</summary>
    Compatibilidad = 3
}

/// <summary>Un envío compatible con el viaje (una fila de la pantalla Oportunidades).</summary>
public sealed class OportunidadResponse
{
    public int EnvioId { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public decimal PesoKg { get; set; }
    public TamanoPaquete Tamano { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Pago { get; set; }
    public decimal DesvioKm { get; set; }
    public TimeSpan TiempoAdicional { get; set; }

    /// <summary>Null cuando el envío no agrega kilómetros.</summary>
    public decimal? GananciaPorKmAdicional { get; set; }

    public NivelCompatibilidad NivelCompatibilidad { get; set; }

    /// <summary>"Muy compatible", "Compatible" o "Mucho desvío".</summary>
    public string Compatibilidad { get; set; } = string.Empty;
}

/// <summary>Comparación entre la ruta habitual del viaje y la ruta llevando el envío (pantalla Detalle de oportunidad).</summary>
public sealed class OportunidadDetalleResponse
{
    public int ViajeId { get; set; }
    public string ViajeOrigen { get; set; } = string.Empty;
    public string ViajeDestino { get; set; } = string.Empty;

    public int EnvioId { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public TamanoPaquete Tamano { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Pago { get; set; }
    public decimal PesoKg { get; set; }

    public decimal DistanciaHabitualKm { get; set; }
    public decimal DistanciaConEnvioKm { get; set; }
    public decimal DesvioKm { get; set; }
    public TimeSpan TiempoHabitual { get; set; }
    public TimeSpan TiempoAdicional { get; set; }

    /// <summary>Recorrido sugerido: origen del viaje, paradas y retiro/entrega del paquete, destino del viaje.</summary>
    public List<string> RecorridoConEnvio { get; set; } = [];

    /// <summary>Paquetes que todavía entran ese día, sin contar este envío.</summary>
    public int PaquetesDisponibles { get; set; }

    /// <summary>Kilos que todavía entran ese día, sin contar este envío.</summary>
    public decimal PesoDisponibleKg { get; set; }

    /// <summary>Kilómetros de desvío que todavía quedan ese día, sin contar este envío.</summary>
    public decimal DesvioDisponibleKm { get; set; }

    /// <summary>Null cuando el envío no agrega kilómetros.</summary>
    public decimal? GananciaPorKmAdicional { get; set; }

    public NivelCompatibilidad NivelCompatibilidad { get; set; }
    public string Compatibilidad { get; set; } = string.Empty;

    /// <summary>False si alguna restricción dejó de cumplirse; los motivos vienen en <see cref="MotivosBloqueo"/>.</summary>
    public bool PuedeAceptarse { get; set; }

    public List<string> MotivosBloqueo { get; set; } = [];
}
