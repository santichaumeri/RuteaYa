using RuteaYa.Logica.Excepciones;

namespace RuteaYa.Logica.Optimizacion.Criterios;

/// <summary>Devuelve la estrategia que corresponde a un <see cref="CriterioOptimizacion"/>.</summary>
public interface ICatalogoCriterios
{
    /// <summary>Lanza ValidacionException si el criterio no existe.</summary>
    ICriterioOptimizacion Obtener(CriterioOptimizacion criterio);

    IReadOnlyCollection<ICriterioOptimizacion> Todos { get; }
}

/// <summary>Arma el catálogo con todas las estrategias registradas en el contenedor.</summary>
public sealed class CatalogoCriterios : ICatalogoCriterios
{
    private readonly Dictionary<CriterioOptimizacion, ICriterioOptimizacion> _criterios;

    public CatalogoCriterios(IEnumerable<ICriterioOptimizacion> criterios)
    {
        _criterios = criterios.ToDictionary(criterio => criterio.Tipo);
    }

    public IReadOnlyCollection<ICriterioOptimizacion> Todos => _criterios.Values;

    public ICriterioOptimizacion Obtener(CriterioOptimizacion criterio) =>
        _criterios.TryGetValue(criterio, out var estrategia)
            ? estrategia
            : throw new ValidacionException(
                $"El criterio de optimización '{criterio}' no es válido. Usá MaximizarGanancias, MinimizarDesvios o Equilibrio.");
}
