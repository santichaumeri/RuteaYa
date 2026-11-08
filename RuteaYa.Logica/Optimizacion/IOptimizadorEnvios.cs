using RuteaYa.Logica.Optimizacion.Criterios;

namespace RuteaYa.Logica.Optimizacion;

public interface IOptimizadorEnvios
{
    /// <summary>
    /// Busca la mejor combinación de candidatos según <paramref name="criterio"/> sin superar los límites del viaje.
    /// Sin criterio usa <see cref="CriterioMaximizarGanancias"/> (máxima ganancia, desempate por menor desvío).
    /// Lanza ValidacionException si los candidatos o las restricciones son inválidos.
    /// </summary>
    ResultadoOptimizacion Optimizar(
        IReadOnlyCollection<CandidatoEnvio> candidatos,
        RestriccionesViaje restricciones,
        ICriterioOptimizacion? criterio = null);
}
