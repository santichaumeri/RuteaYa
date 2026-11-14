using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos.Optimizacion;
using RuteaYa.Logica.Dtos.Potencial;
using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Oportunidades;
using RuteaYa.Logica.Optimizacion.Criterios;

namespace RuteaYa.Logica.Servicios;

public interface IPotencialMensualService
{
    /// <summary>
    /// Estima la ganancia del mes con los viajes recurrentes activos del usuario (HU-20).
    /// Lanza ValidacionException si el mes o el criterio no son válidos, o si el mes ya terminó.
    /// </summary>
    Task<PotencialMensualResponse> CalcularAsync(
        int usuarioId,
        int anio,
        int mes,
        CriterioOptimizacion criterio,
        CancellationToken cancellationToken);
}

/// <summary>
/// Expande los viajes recurrentes a fechas concretas del mes y optimiza cada salida con
/// las oportunidades (HU-11), el optimizador (HU-13) y el criterio elegido (HU-15).
/// Las salidas se procesan en orden cronológico y un envío elegido para una salida
/// ya no se ofrece a las siguientes, así ningún envío se cuenta dos veces.
/// No persiste nada: es una simulación.
/// </summary>
public sealed class PotencialMensualService(
    RuteaYaDbContext dbContext,
    IOportunidadService oportunidadService,
    IOptimizacionService optimizacionService,
    ICatalogoCriterios catalogoCriterios,
    TimeProvider reloj) : IPotencialMensualService
{
    public async Task<PotencialMensualResponse> CalcularAsync(
        int usuarioId,
        int anio,
        int mes,
        CriterioOptimizacion criterio,
        CancellationToken cancellationToken)
    {
        if (mes is < 1 or > 12)
        {
            throw new ValidacionException("El mes debe estar entre 1 y 12.");
        }

        if (anio is < 2000 or > 2100)
        {
            throw new ValidacionException("El año no es válido.");
        }

        var criterioElegido = catalogoCriterios.Obtener(criterio);

        var hoy = DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);
        var primerDia = new DateOnly(anio, mes, 1);
        var ultimoDia = primerDia.AddMonths(1).AddDays(-1);

        if (ultimoDia < hoy)
        {
            throw new ValidacionException("Ese mes ya terminó. Elegí el mes actual o uno futuro.");
        }

        // Los días que ya pasaron no se pueden transportar.
        var desde = primerDia > hoy ? primerDia : hoy;

        var viajes = await dbContext.Viajes
            .AsNoTracking()
            .Where(viaje => viaje.UsuarioId == usuarioId && viaje.Estado == EstadoViaje.Activo)
            .ToListAsync(cancellationToken);

        // Cada viaje en cada fecha del período en que sale, en orden cronológico.
        var salidas = viajes
            .SelectMany(viaje => Dias(desde, ultimoDia)
                .Where(fecha => ReglasCompatibilidad.SaleEl(viaje, fecha))
                .Select(fecha => (Viaje: viaje, Fecha: fecha)))
            .OrderBy(salida => salida.Fecha)
            .ThenBy(salida => salida.Viaje.HoraSalida)
            .ThenBy(salida => salida.Viaje.Id)
            .ToList();

        // Una sola búsqueda de oportunidades por viaje: trae todas sus fechas futuras.
        var candidatosPorViaje = new Dictionary<int, CandidatosViaje>();
        foreach (var viaje in salidas.Select(salida => salida.Viaje).DistinctBy(viaje => viaje.Id))
        {
            candidatosPorViaje[viaje.Id] = await oportunidadService.ObtenerCandidatosPorFechaAsync(
                viaje.Id, usuarioId, cancellationToken);
        }

        var usados = new HashSet<int>();
        var detalle = new List<SalidaPotencialResponse>(salidas.Count);

        foreach (var (viaje, fecha) in salidas)
        {
            var mejor = ElegirEnvios(candidatosPorViaje[viaje.Id], fecha, criterio, usados);
            if (mejor is not null)
            {
                usados.UnionWith(mejor.EnvioIds);
            }

            detalle.Add(new SalidaPotencialResponse
            {
                ViajeId = viaje.Id,
                Origen = viaje.Origen,
                Destino = viaje.Destino,
                Fecha = fecha,
                EnvioIds = mejor?.EnvioIds ?? [],
                Paquetes = mejor?.CantidadPaquetes ?? 0,
                PesoKg = mejor?.PesoTotalKg ?? 0,
                DesvioKm = mejor?.DesvioTotalKm ?? 0,
                Ganancia = mejor?.GananciaTotal ?? 0
            });
        }

        var gananciaPotencial = detalle.Sum(salida => salida.Ganancia);

        return new PotencialMensualResponse
        {
            Anio = anio,
            Mes = mes,
            Criterio = criterioElegido.Tipo,
            CriterioDescripcion = criterioElegido.Descripcion,
            Desde = desde,
            Hasta = ultimoDia,
            ViajesRecurrentes = viajes.Count,
            ViajesAnalizados = detalle.Count,
            PaquetesPosibles = detalle.Sum(salida => salida.Paquetes),
            GananciaPotencial = gananciaPotencial,
            PromedioPorViaje = detalle.Count == 0 ? 0 : Math.Round(gananciaPotencial / detalle.Count, 2),
            GananciaPorSemana = AgruparPorSemana(detalle, desde, ultimoDia),
            MejorRecorrido = detalle
                .Where(salida => salida.Ganancia > 0)
                .OrderByDescending(salida => salida.Ganancia)
                .ThenBy(salida => salida.DesvioKm)
                .ThenBy(salida => salida.Fecha)
                .FirstOrDefault(),
            Detalle = detalle,
            GananciaEfectiva = await CalcularGananciaEfectivaAsync(usuarioId, primerDia, ultimoDia, cancellationToken)
        };
    }

    /// <summary>
    /// Mejor combinación para el viaje en esa fecha, sin los envíos ya elegidos para salidas anteriores.
    /// Null si no hay envíos libres o el viaje ya no tiene lugar ese día.
    /// </summary>
    private CombinacionResponse? ElegirEnvios(
        CandidatosViaje candidatos,
        DateOnly fecha,
        CriterioOptimizacion criterio,
        HashSet<int> usados)
    {
        var libres = candidatos.Fechas
            .FirstOrDefault(grupo => grupo.Fecha == fecha)?
            .Oportunidades
            .Where(oportunidad => !usados.Contains(oportunidad.EnvioId))
            .ToList() ?? [];

        var disponible = candidatos.CapacidadEn(fecha);

        // El optimizador exige al menos un paquete y algo de peso; con el viaje lleno no hay nada que calcular.
        if (libres.Count == 0 || disponible.Paquetes <= 0 || disponible.PesoKg <= 0)
        {
            return null;
        }

        var resultado = optimizacionService.Calcular(new OptimizarRequest
        {
            MaxPaquetes = disponible.Paquetes,
            PesoMaximoKg = disponible.PesoKg,
            DesvioMaximoKm = Math.Max(0, disponible.DesvioKm),
            Criterio = criterio,
            Candidatos = libres
                .Select(oportunidad => new CandidatoEnvioRequest
                {
                    EnvioId = oportunidad.EnvioId,
                    Pago = oportunidad.Pago,
                    PesoKg = oportunidad.PesoKg,
                    DesvioKm = oportunidad.DesvioKm
                })
                .ToList()
        });

        return resultado.MejorCombinacion;
    }

    /// <summary>Lo que el conductor ya ganó ese mes con envíos entregados en sus viajes.</summary>
    private Task<decimal> CalcularGananciaEfectivaAsync(
        int usuarioId,
        DateOnly primerDia,
        DateOnly ultimoDia,
        CancellationToken cancellationToken) =>
        dbContext.Asignaciones
            .AsNoTracking()
            .Where(asignacion => asignacion.Viaje.UsuarioId == usuarioId
                && asignacion.Envio.Estado == EstadoEnvio.Entregado
                && asignacion.Envio.Fecha >= primerDia
                && asignacion.Envio.Fecha <= ultimoDia)
            .SumAsync(asignacion => asignacion.Ganancia, cancellationToken);

    /// <summary>Semanas de lunes a domingo, recortadas al período analizado. Incluye las semanas en cero.</summary>
    private static List<GananciaSemanaResponse> AgruparPorSemana(
        List<SalidaPotencialResponse> detalle,
        DateOnly desde,
        DateOnly hasta)
    {
        var semanas = new List<GananciaSemanaResponse>();
        var inicio = desde;

        while (inicio <= hasta)
        {
            var diasHastaDomingo = ((int)DayOfWeek.Sunday - (int)inicio.DayOfWeek + 7) % 7;
            var fin = inicio.AddDays(diasHastaDomingo);
            if (fin > hasta)
            {
                fin = hasta;
            }

            var deLaSemana = detalle.Where(salida => salida.Fecha >= inicio && salida.Fecha <= fin).ToList();
            semanas.Add(new GananciaSemanaResponse
            {
                Desde = inicio,
                Hasta = fin,
                Paquetes = deLaSemana.Sum(salida => salida.Paquetes),
                Ganancia = deLaSemana.Sum(salida => salida.Ganancia)
            });

            inicio = fin.AddDays(1);
        }

        return semanas;
    }

    private static IEnumerable<DateOnly> Dias(DateOnly desde, DateOnly hasta)
    {
        for (var dia = desde; dia <= hasta; dia = dia.AddDays(1))
        {
            yield return dia;
        }
    }
}
