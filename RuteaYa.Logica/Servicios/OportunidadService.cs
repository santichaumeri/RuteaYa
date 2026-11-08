using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos.Oportunidades;
using RuteaYa.Logica.Dtos.Rutas;
using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Oportunidades;
using RuteaYa.Logica.Servicios.Rutas;

namespace RuteaYa.Logica.Servicios;

/// <summary>
/// Busca envíos compatibles con un viaje y explica la compatibilidad de cada uno.
/// Búsqueda, detalle y verificación evalúan con el mismo método (<see cref="EvaluarAsync"/>),
/// así un envío que aparece en el listado muestra los mismos números en el detalle.
/// </summary>
public sealed class OportunidadService(
    RuteaYaDbContext dbContext,
    IRoutingService routingService,
    TimeProvider reloj) : IOportunidadService
{
    public async Task<IReadOnlyList<OportunidadResponse>> BuscarAsync(
        int viajeId,
        int usuarioId,
        OrdenOportunidades orden,
        CancellationToken cancellationToken)
    {
        var viaje = await ObtenerViajePropioAsync(viajeId, usuarioId, cancellationToken);

        if (viaje.Estado != EstadoViaje.Activo)
        {
            throw new ConflictException("El viaje está inactivo. Activalo para buscar oportunidades.");
        }

        var hoy = Hoy();
        var ocupacion = await ObtenerOcupacionAsync(viaje, cancellationToken);

        // Primer filtro en la base: solo envíos disponibles, ajenos, futuros y que entran por peso.
        var envios = await dbContext.Envios
            .AsNoTracking()
            .Where(envio => envio.Estado == EstadoEnvio.BuscandoConductor
                && envio.UsuarioId != usuarioId
                && envio.Fecha >= hoy
                && envio.PesoKg <= viaje.PesoMaximoKg)
            .ToListAsync(cancellationToken);

        var oportunidades = new List<OportunidadResponse>();

        foreach (var envio in envios.Where(envio => ReglasCompatibilidad.CoincideFecha(viaje, envio.Fecha, hoy)))
        {
            var evaluacion = await EvaluarAsync(viaje, envio, usuarioId, ocupacion, hoy, cancellationToken);

            if (evaluacion.Motivos.Count == 0)
            {
                oportunidades.Add(AOportunidad(envio, evaluacion));
            }
        }

        return Ordenar(oportunidades, orden);
    }

    public async Task<OportunidadDetalleResponse> ObtenerDetalleAsync(
        int viajeId,
        int envioId,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var viaje = await ObtenerViajePropioAsync(viajeId, usuarioId, cancellationToken);
        var envio = await ObtenerEnvioAsync(envioId, cancellationToken);
        var ocupacion = await ObtenerOcupacionAsync(viaje, cancellationToken);

        var evaluacion = await EvaluarAsync(viaje, envio, usuarioId, ocupacion, Hoy(), cancellationToken);

        return ADetalle(viaje, envio, evaluacion);
    }

    public async Task VerificarCompatibilidadAsync(
        int viajeId,
        int envioId,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var detalle = await ObtenerDetalleAsync(viajeId, envioId, usuarioId, cancellationToken);

        if (!detalle.PuedeAceptarse)
        {
            throw new ConflictException(
                "El envío ya no es compatible con el viaje. " + string.Join(" ", detalle.MotivosBloqueo));
        }
    }

    private async Task<Evaluacion> EvaluarAsync(
        Viaje viaje,
        Envio envio,
        int conductorId,
        IReadOnlyDictionary<DateOnly, CapacidadDisponible> ocupacion,
        DateOnly hoy,
        CancellationToken cancellationToken)
    {
        var capacidad = ocupacion.GetValueOrDefault(
            envio.Fecha,
            new CapacidadDisponible(viaje.MaxPaquetes, viaje.PesoMaximoKg, viaje.DesvioMaximoKm));

        var motivos = ReglasCompatibilidad.MotivosSinRuta(viaje, envio, conductorId, capacidad, hoy);

        cancellationToken.ThrowIfCancellationRequested();

        // El servicio de rutas respeta el orden del recorrido: el viaje arranca en su origen,
        // pasa por sus paradas y termina en su destino; el paquete se retira antes de entregarse.
        var ruta = await routingService.CalcularDesvioAsync(
            viaje.Origen,
            viaje.Paradas.OrderBy(parada => parada.Orden).Select(parada => parada.Direccion).ToList(),
            viaje.Destino,
            envio.Origen,
            envio.Destino);

        var desvioKm = AKm(ruta.DesvioKm);

        if (ReglasCompatibilidad.MotivoDesvio(desvioKm, capacidad) is { } motivoDesvio)
        {
            motivos.Add(motivoDesvio);
        }

        var nivel = ReglasCompatibilidad.Clasificar(desvioKm, viaje.DesvioMaximoKm);

        return new Evaluacion(ruta, desvioKm, capacidad, nivel, motivos);
    }

    /// <summary>Capacidad que queda en cada fecha, descontando los envíos ya asignados al viaje (no cancelados).</summary>
    private async Task<IReadOnlyDictionary<DateOnly, CapacidadDisponible>> ObtenerOcupacionAsync(
        Viaje viaje,
        CancellationToken cancellationToken)
    {
        var asignados = await dbContext.Asignaciones
            .AsNoTracking()
            .Where(asignacion => asignacion.ViajeId == viaje.Id
                && asignacion.Envio.Estado != EstadoEnvio.Cancelado)
            .Select(asignacion => new
            {
                asignacion.Envio.Fecha,
                asignacion.Envio.PesoKg,
                asignacion.DesvioKm
            })
            .ToListAsync(cancellationToken);

        return asignados
            .GroupBy(asignado => asignado.Fecha)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => new CapacidadDisponible(
                    viaje.MaxPaquetes - grupo.Count(),
                    viaje.PesoMaximoKg - grupo.Sum(asignado => asignado.PesoKg),
                    viaje.DesvioMaximoKm - grupo.Sum(asignado => asignado.DesvioKm)));
    }

    private async Task<Viaje> ObtenerViajePropioAsync(
        int viajeId,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var viaje = await dbContext.Viajes
            .AsNoTracking()
            .Include(x => x.Paradas)
            .SingleOrDefaultAsync(x => x.Id == viajeId, cancellationToken)
            ?? throw new NotFoundException("El viaje no existe.");

        if (viaje.UsuarioId != usuarioId)
        {
            throw new AccesoDenegadoException("No tenés permiso para acceder a este viaje.");
        }

        return viaje;
    }

    private async Task<Envio> ObtenerEnvioAsync(int envioId, CancellationToken cancellationToken) =>
        await dbContext.Envios
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == envioId, cancellationToken)
        ?? throw new NotFoundException("El envío no existe.");

    private DateOnly Hoy() => DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);

    private static decimal AKm(double km) => Math.Round((decimal)km, 2);

    private static IReadOnlyList<OportunidadResponse> Ordenar(
        IEnumerable<OportunidadResponse> oportunidades,
        OrdenOportunidades orden) => orden switch
    {
        OrdenOportunidades.Desvio => oportunidades
            .OrderBy(x => x.DesvioKm)
            .ThenByDescending(x => x.Pago)
            .ThenBy(x => x.EnvioId)
            .ToList(),
        OrdenOportunidades.Compatibilidad => oportunidades
            .OrderBy(x => x.NivelCompatibilidad)
            // Sin desvío (ganancia por km null) es lo más conveniente dentro de su nivel.
            .ThenByDescending(x => x.GananciaPorKmAdicional ?? decimal.MaxValue)
            .ThenBy(x => x.EnvioId)
            .ToList(),
        _ => oportunidades
            .OrderByDescending(x => x.Pago)
            .ThenBy(x => x.DesvioKm)
            .ThenBy(x => x.EnvioId)
            .ToList()
    };

    private static OportunidadResponse AOportunidad(Envio envio, Evaluacion evaluacion) => new()
    {
        EnvioId = envio.Id,
        Origen = envio.Origen,
        Destino = envio.Destino,
        Fecha = envio.Fecha,
        PesoKg = envio.PesoKg,
        Tamano = envio.Tamano,
        Descripcion = envio.Descripcion,
        Pago = envio.MontoOfrecido,
        DesvioKm = evaluacion.DesvioKm,
        TiempoAdicional = evaluacion.Ruta.TiempoAdicional,
        GananciaPorKmAdicional = ReglasCompatibilidad.GananciaPorKmAdicional(envio.MontoOfrecido, evaluacion.DesvioKm),
        NivelCompatibilidad = evaluacion.Nivel,
        Compatibilidad = ReglasCompatibilidad.Etiqueta(evaluacion.Nivel)
    };

    private static OportunidadDetalleResponse ADetalle(Viaje viaje, Envio envio, Evaluacion evaluacion) => new()
    {
        ViajeId = viaje.Id,
        ViajeOrigen = viaje.Origen,
        ViajeDestino = viaje.Destino,
        EnvioId = envio.Id,
        Origen = envio.Origen,
        Destino = envio.Destino,
        Fecha = envio.Fecha,
        Tamano = envio.Tamano,
        Descripcion = envio.Descripcion,
        Pago = envio.MontoOfrecido,
        PesoKg = envio.PesoKg,
        DistanciaHabitualKm = AKm(evaluacion.Ruta.DistanciaHabitualKm),
        DistanciaConEnvioKm = AKm(evaluacion.Ruta.DistanciaConEnvioKm),
        DesvioKm = evaluacion.DesvioKm,
        TiempoHabitual = evaluacion.Ruta.DuracionHabitual,
        TiempoAdicional = evaluacion.Ruta.TiempoAdicional,
        RecorridoConEnvio = evaluacion.Ruta.RecorridoFinal,
        PaquetesDisponibles = Math.Max(0, evaluacion.Capacidad.Paquetes),
        PesoDisponibleKg = Math.Max(0, evaluacion.Capacidad.PesoKg),
        DesvioDisponibleKm = Math.Max(0, evaluacion.Capacidad.DesvioKm),
        GananciaPorKmAdicional = ReglasCompatibilidad.GananciaPorKmAdicional(envio.MontoOfrecido, evaluacion.DesvioKm),
        NivelCompatibilidad = evaluacion.Nivel,
        Compatibilidad = ReglasCompatibilidad.Etiqueta(evaluacion.Nivel),
        PuedeAceptarse = evaluacion.Motivos.Count == 0,
        MotivosBloqueo = evaluacion.Motivos
    };

    private sealed record Evaluacion(
        CalculoDesvioDto Ruta,
        decimal DesvioKm,
        CapacidadDisponible Capacidad,
        NivelCompatibilidad Nivel,
        List<string> Motivos);
}
