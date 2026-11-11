using RuteaYa.Logica.Dtos.Optimizacion;
using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Optimizacion;
using RuteaYa.Logica.Optimizacion.Criterios;

namespace RuteaYa.Logica.Servicios;

/// <summary>
/// Traduce el request de la API al modelo del optimizador y el resultado a DTOs.
/// La lógica de combinación vive en <see cref="IOptimizadorEnvios"/> y el criterio en <see cref="ICatalogoCriterios"/>.
/// </summary>
public sealed class OptimizacionService(
    IOptimizadorEnvios optimizador,
    ICatalogoCriterios criterios) : IOptimizacionService
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

        var criterio = criterios.Obtener(request.Criterio);
        var resultado = optimizador.Optimizar(candidatos, restricciones, criterio);
        CombinacionResponse ADto(Combinacion combinacion) => AResponse(combinacion, criterio);

        return new OptimizacionResponse
        {
            Criterio = criterio.Tipo,
            CriterioDescripcion = criterio.Descripcion,
            MejorCombinacion = resultado.MejorCombinacion is null ? null : ADto(resultado.MejorCombinacion),
            Alternativas = resultado.Alternativas.Select(ADto).ToList(),
            Descartadas = resultado.Descartadas.Select(ADto).ToList(),
            CombinacionesEvaluadas = resultado.CombinacionesEvaluadas
        };
    }

    private static CombinacionResponse AResponse(Combinacion combinacion, ICriterioOptimizacion criterio) => new()
    {
        EnvioIds = combinacion.EnvioIds.ToList(),
        CantidadPaquetes = combinacion.CantidadPaquetes,
        GananciaTotal = combinacion.GananciaTotal,
        PesoTotalKg = combinacion.PesoTotalKg,
        DesvioTotalKm = combinacion.DesvioTotalKm,
        MotivosDescarte = combinacion.MotivosDescarte,
        GananciaNeta = criterio is CriterioEquilibrio equilibrio ? equilibrio.GananciaNeta(combinacion) : null
    };
}
