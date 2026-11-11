using System.Globalization;

namespace RuteaYa.Logica.Optimizacion.Criterios;

/// <summary>Mayor ganancia; ante empate, menor desvío. Respeta la regla del enunciado y es el criterio por defecto.</summary>
public sealed class CriterioMaximizarGanancias : ICriterioOptimizacion
{
    public static readonly CriterioMaximizarGanancias Instancia = new();

    public CriterioOptimizacion Tipo => CriterioOptimizacion.MaximizarGanancias;

    public string Descripcion => "Maximiza la ganancia total; si dos combinaciones ganan lo mismo, elige la de menor desvío.";

    public int Comparar(Combinacion a, Combinacion b)
    {
        var resultado = b.GananciaTotal.CompareTo(a.GananciaTotal);
        if (resultado != 0) return resultado;

        resultado = a.DesvioTotalKm.CompareTo(b.DesvioTotalKm);
        return resultado != 0 ? resultado : Desempate.Comparar(a, b);
    }
}

/// <summary>
/// Menor desvío total; ante empate, mayor ganancia. Para el conductor que no quiere apartarse de su ruta:
/// suma todos los envíos que no lo desvían y, si ninguno es gratis, elige el que menos lo aparta.
/// </summary>
public sealed class CriterioMinimizarDesvios : ICriterioOptimizacion
{
    public CriterioOptimizacion Tipo => CriterioOptimizacion.MinimizarDesvios;

    public string Descripcion => "Minimiza los kilómetros de desvío; si dos combinaciones desvían lo mismo, elige la de mayor ganancia.";

    public int Comparar(Combinacion a, Combinacion b)
    {
        var resultado = a.DesvioTotalKm.CompareTo(b.DesvioTotalKm);
        if (resultado != 0) return resultado;

        resultado = b.GananciaTotal.CompareTo(a.GananciaTotal);
        return resultado != 0 ? resultado : Desempate.Comparar(a, b);
    }
}

/// <summary>
/// Balance entre ganancia y desvío con una ponderación configurable:
/// <c>ganancia neta = ganancia total − costo por km × desvío total</c>.
/// </summary>
/// <remarks>
/// El costo por km representa lo que le cuesta al conductor cada km extra (combustible, tiempo).
/// Como el desvío es la suma de los desvíos de cada envío, la ganancia neta también se suma por envío:
/// un envío que paga menos que el costo de su desvío baja el puntaje y queda afuera.
/// Con costo 0 equivale a <see cref="CriterioMaximizarGanancias"/>. Ante empate de ganancia neta, menor desvío.
/// </remarks>
public sealed class CriterioEquilibrio : ICriterioOptimizacion
{
    public const decimal CostoPorKmPorDefecto = 1000m;

    public CriterioEquilibrio(decimal costoPorKm = CostoPorKmPorDefecto)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(costoPorKm);
        CostoPorKm = costoPorKm;
    }

    public decimal CostoPorKm { get; }

    public CriterioOptimizacion Tipo => CriterioOptimizacion.Equilibrio;

    public string Descripcion => string.Format(
        CultureInfo.GetCultureInfo("es-AR"),
        "Maximiza la ganancia neta: ganancia − {0:C0} por cada km de desvío; si empatan, elige la de menor desvío.",
        CostoPorKm);

    public decimal GananciaNeta(Combinacion combinacion) =>
        combinacion.GananciaTotal - CostoPorKm * combinacion.DesvioTotalKm;

    public int Comparar(Combinacion a, Combinacion b)
    {
        var resultado = GananciaNeta(b).CompareTo(GananciaNeta(a));
        if (resultado != 0) return resultado;

        resultado = a.DesvioTotalKm.CompareTo(b.DesvioTotalKm);
        return resultado != 0 ? resultado : Desempate.Comparar(a, b);
    }
}

/// <summary>Desempate común a todos los criterios para que el resultado sea determinístico.</summary>
internal static class Desempate
{
    /// <summary>Menos paquetes, después menos peso y, por último, los ids de los envíos.</summary>
    public static int Comparar(Combinacion a, Combinacion b)
    {
        var resultado = a.CantidadPaquetes.CompareTo(b.CantidadPaquetes);
        if (resultado != 0) return resultado;

        resultado = a.PesoTotalKg.CompareTo(b.PesoTotalKg);
        return resultado != 0 ? resultado : CompararIds(a, b);
    }

    public static int CompararIds(Combinacion a, Combinacion b)
    {
        for (var i = 0; i < Math.Min(a.CantidadPaquetes, b.CantidadPaquetes); i++)
        {
            var resultado = a.Envios[i].EnvioId.CompareTo(b.Envios[i].EnvioId);
            if (resultado != 0) return resultado;
        }

        return a.CantidadPaquetes.CompareTo(b.CantidadPaquetes);
    }
}
