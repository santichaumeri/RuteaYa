using RuteaYa.Logica.Dtos.Optimizacion;
using RuteaYa.Logica.Optimizacion.Criterios;

namespace RuteaYa.Logica.Servicios;

public interface IOptimizacionViajeService
{
    /// <summary>
    /// Arma la mejor combinación de oportunidades para un viaje del usuario en una fecha (HU-14).
    /// Sin fecha usa la más próxima que tenga oportunidades. Lanza NotFoundException si el viaje no existe,
    /// AccesoDenegadoException si es de otro usuario y ConflictException si está inactivo.
    /// </summary>
    Task<OptimizacionViajeResponse> CalcularAsync(
        int viajeId,
        int usuarioId,
        DateOnly? fecha,
        CriterioOptimizacion criterio,
        CancellationToken cancellationToken);
}

/// <summary>
/// Une las oportunidades del viaje (HU-11) con el optimizador (HU-13) y sus criterios (HU-15).
/// Cada fecha se optimiza por separado porque la capacidad del viaje es por día.
/// </summary>
public sealed class OptimizacionViajeService(
    IOportunidadService oportunidadService,
    IOptimizacionService optimizacionService) : IOptimizacionViajeService
{
    public async Task<OptimizacionViajeResponse> CalcularAsync(
        int viajeId,
        int usuarioId,
        DateOnly? fecha,
        CriterioOptimizacion criterio,
        CancellationToken cancellationToken)
    {
        var candidatos = await oportunidadService.ObtenerCandidatosPorFechaAsync(viajeId, usuarioId, cancellationToken);

        var fechaElegida = fecha ?? candidatos.Fechas.FirstOrDefault()?.Fecha;
        var delDia = candidatos.Fechas.FirstOrDefault(grupo => grupo.Fecha == fechaElegida);
        var completa = candidatos.CapacidadCompleta;
        var disponible = fechaElegida is { } dia ? candidatos.CapacidadEn(dia) : completa;

        OptimizacionResponse? resultado = null;
        if (delDia is not null)
        {
            // Las restricciones son lo que le queda al viaje ese día, no su capacidad total.
            resultado = optimizacionService.Calcular(new OptimizarRequest
            {
                MaxPaquetes = disponible.Paquetes,
                PesoMaximoKg = disponible.PesoKg,
                DesvioMaximoKm = disponible.DesvioKm,
                Criterio = criterio,
                Candidatos = delDia.Oportunidades
                    .Select(oportunidad => new CandidatoEnvioRequest
                    {
                        EnvioId = oportunidad.EnvioId,
                        Pago = oportunidad.Pago,
                        PesoKg = oportunidad.PesoKg,
                        DesvioKm = oportunidad.DesvioKm
                    })
                    .ToList()
            });
        }

        return new OptimizacionViajeResponse
        {
            ViajeId = candidatos.ViajeId,
            ViajeOrigen = candidatos.Origen,
            ViajeDestino = candidatos.Destino,
            MaxPaquetes = completa.Paquetes,
            PesoMaximoKg = completa.PesoKg,
            DesvioMaximoKm = completa.DesvioKm,
            FechasConOportunidades = candidatos.Fechas.Select(grupo => grupo.Fecha).ToList(),
            Fecha = fechaElegida,
            PaquetesAsignados = completa.Paquetes - disponible.Paquetes,
            PesoAsignadoKg = completa.PesoKg - disponible.PesoKg,
            DesvioAsignadoKm = completa.DesvioKm - disponible.DesvioKm,
            Candidatos = delDia?.Oportunidades ?? [],
            Resultado = resultado
        };
    }
}
