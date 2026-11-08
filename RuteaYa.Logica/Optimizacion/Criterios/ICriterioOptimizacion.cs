using System.Text.Json.Serialization;

namespace RuteaYa.Logica.Optimizacion.Criterios;

/// <summary>Qué prioriza el optimizador al elegir entre combinaciones válidas (HU-15).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CriterioOptimizacion>))]
public enum CriterioOptimizacion
{
    /// <summary>Mayor ganancia; ante empate, menor desvío (regla del enunciado). Es el criterio por defecto.</summary>
    MaximizarGanancias = 1,

    /// <summary>Menor desvío total; ante empate, mayor ganancia.</summary>
    MinimizarDesvios = 2,

    /// <summary>Mayor ganancia neta: ganancia − costo por km × desvío; ante empate, menor desvío.</summary>
    Equilibrio = 3
}

/// <summary>
/// Estrategia de comparación del optimizador (patrón Strategy).
/// El optimizador recorre siempre todas las combinaciones que respetan los límites del viaje;
/// el criterio solo decide cuál es mejor. Para agregar uno nuevo alcanza con implementar esta
/// interfaz y registrarla en el contenedor de dependencias.
/// </summary>
public interface ICriterioOptimizacion
{
    CriterioOptimizacion Tipo { get; }

    /// <summary>Explicación para mostrar al usuario.</summary>
    string Descripcion { get; }

    /// <summary>Negativo si <paramref name="a"/> es preferible a <paramref name="b"/>, positivo si es peor, 0 si son la misma.</summary>
    int Comparar(Combinacion a, Combinacion b);
}
