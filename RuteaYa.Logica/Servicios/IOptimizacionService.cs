using RuteaYa.Logica.Dtos.Optimizacion;

namespace RuteaYa.Logica.Servicios;

/// <summary>Caso de uso de optimización. Lo consume OptimizacionController en la API.</summary>
public interface IOptimizacionService
{
    /// <summary>Calcula la mejor combinación de candidatos. Lanza ValidacionException si los datos son inválidos.</summary>
    OptimizacionResponse Calcular(OptimizarRequest request);
}
