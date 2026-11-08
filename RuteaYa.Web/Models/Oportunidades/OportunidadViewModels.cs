namespace RuteaYa.Web.Models.Oportunidades;

/// <summary>Pantalla 8: oportunidades de un viaje.</summary>
public sealed class OportunidadesViewModel
{
    /// <summary>Viajes del usuario para el selector.</summary>
    public IReadOnlyList<Dashboard.ViajeResumenViewModel> MisViajes { get; set; } = [];

    /// <summary>Viaje elegido; null mientras no se eligió ninguno.</summary>
    public ViajeDetalleViewModel? Viaje { get; set; }
    public string Orden { get; set; } = "Ganancia";
    public IReadOnlyList<OportunidadViewModel> Oportunidades { get; set; } = [];
    public string? Error { get; set; }
}

public sealed class OportunidadViewModel
{
    public int EnvioId { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public decimal PesoKg { get; set; }
    public int Tamano { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Pago { get; set; }
    public decimal DesvioKm { get; set; }
    public TimeSpan TiempoAdicional { get; set; }
    public decimal? GananciaPorKmAdicional { get; set; }
    public int NivelCompatibilidad { get; set; }
    public string Compatibilidad { get; set; } = string.Empty;
}

/// <summary>Pantalla 9: detalle de una oportunidad.</summary>
public sealed class OportunidadDetalleViewModel
{
    public int ViajeId { get; set; }
    public string ViajeOrigen { get; set; } = string.Empty;
    public string ViajeDestino { get; set; } = string.Empty;

    public int EnvioId { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public int Tamano { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Pago { get; set; }
    public decimal PesoKg { get; set; }

    public decimal DistanciaHabitualKm { get; set; }
    public decimal DistanciaConEnvioKm { get; set; }
    public decimal DesvioKm { get; set; }
    public TimeSpan TiempoHabitual { get; set; }
    public TimeSpan TiempoAdicional { get; set; }
    public List<string> RecorridoConEnvio { get; set; } = [];

    public int PaquetesDisponibles { get; set; }
    public decimal PesoDisponibleKg { get; set; }
    public decimal DesvioDisponibleKm { get; set; }

    public decimal? GananciaPorKmAdicional { get; set; }
    public int NivelCompatibilidad { get; set; }
    public string Compatibilidad { get; set; } = string.Empty;

    public bool PuedeAceptarse { get; set; }
    public List<string> MotivosBloqueo { get; set; } = [];
}

public static class OportunidadFormato
{
    public static string Tamano(int tamano) => tamano switch
    {
        1 => "Pequeño",
        2 => "Mediano",
        3 => "Grande",
        _ => "-"
    };

    /// <summary>Clase de Bootstrap para la etiqueta de compatibilidad.</summary>
    public static string ClaseCompatibilidad(int nivel) => nivel switch
    {
        1 => "bg-success",
        2 => "bg-primary",
        _ => "bg-warning text-dark"
    };

    public static string Minutos(TimeSpan tiempo) =>
        tiempo.TotalMinutes < 60
            ? $"{Math.Round(tiempo.TotalMinutes)} min"
            : $"{(int)tiempo.TotalHours} h {tiempo.Minutes} min";
}
