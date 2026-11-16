using System.Globalization;
using RuteaYa.Web.Models.Optimizador;

namespace RuteaYa.Web.Models.Potencial;

/// <summary>Pantalla 14: potencial de ganancias mensual (HU-21).</summary>
public sealed class PotencialPaginaViewModel
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public string Criterio { get; set; } = CriteriosOptimizacion.PorDefecto;

    /// <summary>Primer mes que se puede elegir (el actual): los meses que ya terminaron no se pueden estimar.</summary>
    public string PeriodoMinimo { get; set; } = string.Empty;

    /// <summary>Null si la API respondió con error.</summary>
    public PotencialMensualViewModel? Datos { get; set; }

    public string? Error { get; set; }

    /// <summary>Valor del input type="month" (yyyy-MM).</summary>
    public string Periodo => $"{Anio:D4}-{Mes:D2}";

    public string NombreMes => new DateOnly(Anio, Mes, 1).ToString("MMMM 'de' yyyy", PotencialFormato.Cultura);
}

/// <summary>Respuesta de GET api/potencial.</summary>
public sealed class PotencialMensualViewModel
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public string Criterio { get; set; } = string.Empty;
    public string CriterioDescripcion { get; set; } = string.Empty;
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public int ViajesRecurrentes { get; set; }
    public int ViajesAnalizados { get; set; }
    public int PaquetesPosibles { get; set; }
    public decimal GananciaPotencial { get; set; }
    public decimal PromedioPorViaje { get; set; }
    public List<GananciaSemanaViewModel> GananciaPorSemana { get; set; } = [];
    public SalidaPotencialViewModel? MejorRecorrido { get; set; }
    public List<SalidaPotencialViewModel> Detalle { get; set; } = [];
    public decimal GananciaEfectiva { get; set; }
    public bool EsSimulacion { get; set; }
}

public sealed class SalidaPotencialViewModel
{
    public int ViajeId { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public List<int> EnvioIds { get; set; } = [];
    public List<EnvioPotencialViewModel> Envios { get; set; } = [];
    public int Paquetes { get; set; }
    public decimal PesoKg { get; set; }
    public decimal DesvioKm { get; set; }
    public decimal Ganancia { get; set; }
}

public sealed class EnvioPotencialViewModel
{
    public int EnvioId { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal PesoKg { get; set; }
    public decimal DesvioKm { get; set; }
    public decimal Pago { get; set; }
}

public sealed class GananciaSemanaViewModel
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public int Paquetes { get; set; }
    public decimal Ganancia { get; set; }
}

public static class PotencialFormato
{
    public static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

    public static string Pesos(decimal monto) => monto.ToString("C0", Cultura);

    public static string Fecha(DateOnly fecha) => fecha.ToString("ddd dd/MM", Cultura);

    public static string Rango(DateOnly desde, DateOnly hasta) =>
        desde == hasta
            ? desde.ToString("dd/MM", Cultura)
            : $"{desde.ToString("dd/MM", Cultura)} al {hasta.ToString("dd/MM", Cultura)}";
}
