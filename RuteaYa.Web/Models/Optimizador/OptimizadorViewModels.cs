using RuteaYa.Web.Models.Dashboard;
using RuteaYa.Web.Models.Oportunidades;

namespace RuteaYa.Web.Models.Optimizador;

/// <summary>Pantalla 10: optimizador de oportunidades de un viaje.</summary>
public sealed class OptimizadorPaginaViewModel
{
    public IReadOnlyList<ViajeResumenViewModel> MisViajes { get; set; } = [];
    public string Criterio { get; set; } = CriteriosOptimizacion.PorDefecto;

    /// <summary>Null mientras no se eligió un viaje.</summary>
    public OptimizacionViajeViewModel? Datos { get; set; }

    public string? Error { get; set; }
}

/// <summary>Respuesta de GET api/viajes/{viajeId}/optimizacion.</summary>
public sealed class OptimizacionViajeViewModel
{
    public int ViajeId { get; set; }
    public string ViajeOrigen { get; set; } = string.Empty;
    public string ViajeDestino { get; set; } = string.Empty;
    public int MaxPaquetes { get; set; }
    public decimal PesoMaximoKg { get; set; }
    public decimal DesvioMaximoKm { get; set; }
    public List<DateOnly> FechasConOportunidades { get; set; } = [];
    public DateOnly? Fecha { get; set; }
    public int PaquetesAsignados { get; set; }
    public decimal PesoAsignadoKg { get; set; }
    public decimal DesvioAsignadoKm { get; set; }
    public List<OportunidadViewModel> Candidatos { get; set; } = [];
    public OptimizacionResultadoViewModel? Resultado { get; set; }

    public OportunidadViewModel? Candidato(int envioId) =>
        Candidatos.FirstOrDefault(candidato => candidato.EnvioId == envioId);
}

public sealed class OptimizacionResultadoViewModel
{
    public string Criterio { get; set; } = string.Empty;
    public string CriterioDescripcion { get; set; } = string.Empty;
    public CombinacionViewModel? MejorCombinacion { get; set; }
    public List<CombinacionViewModel> Alternativas { get; set; } = [];
    public List<CombinacionViewModel> Descartadas { get; set; } = [];
    public int CombinacionesEvaluadas { get; set; }
}

public sealed class CombinacionViewModel
{
    public List<int> EnvioIds { get; set; } = [];
    public int CantidadPaquetes { get; set; }
    public decimal GananciaTotal { get; set; }
    public decimal PesoTotalKg { get; set; }
    public decimal DesvioTotalKm { get; set; }
    public decimal? GananciaNeta { get; set; }
    public List<string> MotivosDescarte { get; set; } = [];
}

/// <summary>Respuesta de POST api/asignaciones/combinacion (HU-17): lo que quedó reservado.</summary>
public sealed class CombinacionConfirmadaViewModel
{
    public int ViajeId { get; set; }
    public DateOnly Fecha { get; set; }
    public int CantidadPaquetes { get; set; }
    public decimal GananciaTotal { get; set; }
    public decimal PesoTotalKg { get; set; }
    public decimal DesvioTotalKm { get; set; }
}

/// <summary>Criterios de HU-15 tal como los acepta la API.</summary>
public static class CriteriosOptimizacion
{
    public const string PorDefecto = "MaximizarGanancias";

    public static readonly IReadOnlyList<(string Valor, string Texto)> Todos =
    [
        ("MaximizarGanancias", "Maximizar ganancias"),
        ("MinimizarDesvios", "Minimizar desvíos"),
        ("Equilibrio", "Equilibrio ganancia / desvío")
    ];

    public static string Normalizar(string? criterio) =>
        Todos.FirstOrDefault(c => string.Equals(c.Valor, criterio, StringComparison.OrdinalIgnoreCase)).Valor
        ?? PorDefecto;
}
