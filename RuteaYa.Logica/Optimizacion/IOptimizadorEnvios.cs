namespace RuteaYa.Logica.Optimizacion;

public interface IOptimizadorEnvios
{
    /// <summary>
    /// Busca la combinación de candidatos que maximiza la ganancia sin superar los límites del viaje.
    /// Lanza ValidacionException si los candidatos o las restricciones son inválidos.
    /// </summary>
    ResultadoOptimizacion Optimizar(IReadOnlyCollection<CandidatoEnvio> candidatos, RestriccionesViaje restricciones);
}
