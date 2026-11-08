using RuteaYa.Logica.Dtos.Optimizacion;
using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Optimizacion;

namespace RuteaYa.Logica.Servicios;

/// <summary>
/// Traduce el request de la API al modelo del optimizador y el resultado a DTOs.
/// La lógica de combinación vive en <see cref="IOptimizadorEnvios"/>.
/// </summary>
public sealed class OptimizacionService(IOptimizadorEnvios optimizador) : IOptimizacionService
{
    public OptimizacionResponse Calcular(OptimizarRequest request)
    {
        if (request is null)
        {
            throw new ValidacionException("Los datos de la optimización son obligatorios.");
        }

        var candidatos = (request.Candidatos ?? [])
            .Select(c => new CandidatoEnvio(c.EnvioId, c.Pago, c.PesoKg, c.DesvioKm))
            .ToList();
        var restricciones = new RestriccionesViaje(request.MaxPaquetes, request.PesoMaximoKg, request.DesvioMaximoKm);

        var resultado = optimizador.Optimizar(candidatos, restricciones);

        return new OptimizacionResponse
        {
            MejorCombinacion = resultado.MejorCombinacion is null ? null : ADto(resultado.MejorCombinacion),
            Alternativas = resultado.Alternativas.Select(ADto).ToList(),
            Descartadas = resultado.Descartadas.Select(ADto).ToList(),
            CombinacionesEvaluadas = resultado.CombinacionesEvaluadas
        };
    }

    private static CombinacionResponse ADto(Combinacion combinacion) => new()
    {
        EnvioIds = combinacion.EnvioIds.ToList(),
        CantidadPaquetes = combinacion.CantidadPaquetes,
        GananciaTotal = combinacion.GananciaTotal,
        PesoTotalKg = combinacion.PesoTotalKg,
        DesvioTotalKm = combinacion.DesvioTotalKm,
        MotivosDescarte = combinacion.MotivosDescarte
    };
}
