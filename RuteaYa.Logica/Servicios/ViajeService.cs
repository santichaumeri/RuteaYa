using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos;
using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Servicios.Rutas;

namespace RuteaYa.Logica.Servicios;

/// <summary>
/// Alta, edición y consulta de viajes. La ruta se calcula con <see cref="IRoutingService"/>,
/// así el viaje no depende del proveedor de mapas (o del mock).
/// </summary>
public sealed class ViajeService(
    RuteaYaDbContext dbContext,
    IRoutingService routingService) : IViajeService
{
    private const DiasSemana DiasSemanaValidos =
        DiasSemana.Lunes
        | DiasSemana.Martes
        | DiasSemana.Miercoles
        | DiasSemana.Jueves
        | DiasSemana.Viernes
        | DiasSemana.Sabado
        | DiasSemana.Domingo;

    public async Task<ViajeResponse> CrearViajeAsync(
        int usuarioId,
        CrearViajeRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ValidacionException("Los datos del viaje son obligatorios.");
        }

        var origen = NormalizarDireccion(request.Origen);
        var destino = NormalizarDireccion(request.Destino);

        ValidarDatosGenerales(
            request.DiasSemana,
            request.HoraSalida,
            request.HoraLlegada,
            request.MaxPaquetes,
            request.PesoMaximoKg,
            request.DesvioMaximoKm,
            origen,
            destino);

        var paradas = (request.Paradas ?? [])
            .Select(parada => NormalizarDireccion(parada?.Direccion ?? string.Empty))
            .ToList();

        ReglasParadas.Validar(paradas, origen, destino);

        var viaje = new Viaje
        {
            UsuarioId = usuarioId,
            Origen = origen,
            Destino = destino,
            DiasSemana = request.DiasSemana,
            HoraSalida = TimeOnly.FromTimeSpan(request.HoraSalida),
            HoraLlegada = TimeOnly.FromTimeSpan(request.HoraLlegada),
            MaxPaquetes = request.MaxPaquetes,
            PesoMaximoKg = request.PesoMaximoKg,
            DesvioMaximoKm = request.DesvioMaximoKm,
            Estado = EstadoViaje.Activo,
            Paradas = paradas
                .Select((direccion, indice) => new Parada
                {
                    Direccion = direccion,
                    Orden = indice + 1
                })
                .ToList()
        };

        dbContext.Viajes.Add(viaje);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AResponse(viaje);
    }

    public async Task<ViajeDetalleResponse> ObtenerDetalleAsync(
        int viajeId,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var viaje = await ObtenerViajeConRelacionesAsync(viajeId, cancellationToken);

        ValidarAcceso(viaje, usuarioId);

        return ADetalleResponse(viaje, await CalcularRutaDelViajeAsync(viaje));
    }

    public async Task<IReadOnlyCollection<ViajeResponse>> ObtenerMisViajesAsync(
        int usuarioId,
        EstadoViaje? estado,
        CancellationToken cancellationToken)
    {
        var consulta = dbContext.Viajes
            .AsNoTracking()
            .Include(x => x.Paradas)
            .Where(x => x.UsuarioId == usuarioId);

        if (estado is not null)
        {
            consulta = consulta.Where(x => x.Estado == estado.Value);
        }

        var viajes = await consulta
            .OrderBy(x => x.Origen)
            .ThenBy(x => x.Destino)
            .ThenBy(x => x.HoraSalida)
            .ToListAsync(cancellationToken);

        return viajes.Select(AResponse).ToList();
    }

    public async Task DesactivarViajeAsync(
        int usuarioId,
        int viajeId,
        CancellationToken cancellationToken)
    {
        var viaje = await dbContext.Viajes
            .SingleOrDefaultAsync(x => x.Id == viajeId && x.UsuarioId == usuarioId, cancellationToken);

        if (viaje is null)
        {
            throw new NotFoundException("El viaje no existe o no pertenece al usuario autenticado.");
        }

        if (viaje.Estado == EstadoViaje.Inactivo)
        {
            throw new ConflictException("El viaje ya está inactivo.");
        }

        viaje.Estado = EstadoViaje.Inactivo;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ViajeDetalleResponse> EditarViajeAsync(
        int viajeId,
        int usuarioId,
        EditarViajeRequest request,
        CancellationToken cancellationToken)
    {
        var viaje = await ObtenerViajeConRelacionesAsync(viajeId, cancellationToken);

        ValidarAcceso(viaje, usuarioId);

        if (request is null)
        {
            throw new ValidacionException("Los datos del viaje son obligatorios.");
        }

        var origen = NormalizarDireccion(request.Origen);
        var destino = NormalizarDireccion(request.Destino);

        ValidarDatosGenerales(
            request.DiasSemana,
            request.HoraSalida,
            request.HoraLlegada,
            request.MaxPaquetes,
            request.PesoMaximoKg,
            request.DesvioMaximoKm,
            origen,
            destino);

        var paradas = (request.Paradas ?? [])
            .Select(parada => NormalizarDireccion(parada?.Direccion ?? string.Empty))
            .ToList();

        ReglasParadas.Validar(paradas, origen, destino);

        viaje.Origen = origen;
        viaje.Destino = destino;
        viaje.DiasSemana = request.DiasSemana;
        viaje.HoraSalida = TimeOnly.FromTimeSpan(request.HoraSalida);
        viaje.HoraLlegada = TimeOnly.FromTimeSpan(request.HoraLlegada);
        viaje.MaxPaquetes = request.MaxPaquetes;
        viaje.PesoMaximoKg = request.PesoMaximoKg;
        viaje.DesvioMaximoKm = request.DesvioMaximoKm;

        dbContext.Paradas.RemoveRange(viaje.Paradas);
        viaje.Paradas = paradas
            .Select((direccion, indice) => new Parada
            {
                ViajeId = viaje.Id,
                Direccion = direccion,
                Orden = indice + 1
            })
            .ToList();

        await dbContext.SaveChangesAsync(cancellationToken);

        return ADetalleResponse(viaje, await CalcularRutaDelViajeAsync(viaje));
    }

    public async Task<RutaViajeResponse> CalcularRutaAsync(
        RutaViajeRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ValidacionException("Los datos del recorrido son obligatorios.");
        }

        var origen = NormalizarDireccion(request.Origen);
        var destino = NormalizarDireccion(request.Destino);
        ValidarRecorrido(origen, destino);

        var paradas = (request.Paradas ?? [])
            .Select(NormalizarDireccion)
            .ToList();
        ReglasParadas.Validar(paradas, origen, destino);

        return await CalcularRutaAsync(origen, paradas, destino);
    }

    private Task<RutaViajeResponse> CalcularRutaDelViajeAsync(Viaje viaje) =>
        CalcularRutaAsync(
            viaje.Origen,
            viaje.Paradas.OrderBy(parada => parada.Orden).Select(parada => parada.Direccion).ToList(),
            viaje.Destino);

    private async Task<RutaViajeResponse> CalcularRutaAsync(string origen, List<string> paradas, string destino)
    {
        var ruta = await routingService.CalcularRutaConParadasAsync(origen, paradas, destino);

        return new RutaViajeResponse
        {
            DistanciaKm = Math.Round((decimal)ruta.DistanciaKm, 2),
            DuracionMinutos = (int)Math.Round(ruta.DuracionEstimada.TotalMinutes),
            Puntos = ruta.PuntosRecorridos
        };
    }

    private static void ValidarDatosGenerales(
        DiasSemana diasSemana,
        TimeSpan horaSalida,
        TimeSpan horaLlegada,
        int maxPaquetes,
        decimal pesoMaximoKg,
        decimal desvioMaximoKm,
        string origen,
        string destino)
    {
        ValidarRecorrido(origen, destino);

        if (diasSemana == DiasSemana.Ninguno
            || (diasSemana & ~DiasSemanaValidos) != DiasSemana.Ninguno)
        {
            throw new ValidacionException("Debe seleccionar al menos un día de la semana válido.");
        }

        if (!EsHoraValida(horaSalida))
        {
            throw new ValidacionException("La hora de salida no es válida.");
        }

        if (!EsHoraValida(horaLlegada))
        {
            throw new ValidacionException("La hora de llegada no es válida.");
        }

        // El viaje se registra por día de salida, así que tiene que llegar el mismo día y después de salir.
        if (horaLlegada <= horaSalida)
        {
            throw new ValidacionException("La hora de llegada tiene que ser posterior a la hora de salida.");
        }

        if (maxPaquetes <= 0)
        {
            throw new ValidacionException("La cantidad máxima de paquetes debe ser mayor a cero.");
        }

        if (pesoMaximoKg <= 0)
        {
            throw new ValidacionException("El peso máximo debe ser mayor a cero.");
        }

        if (desvioMaximoKm <= 0)
        {
            throw new ValidacionException("El desvío máximo debe ser mayor a cero.");
        }
    }

    private static void ValidarRecorrido(string origen, string destino)
    {
        if (origen.Length == 0)
        {
            throw new ValidacionException("El origen es obligatorio.");
        }

        if (destino.Length == 0)
        {
            throw new ValidacionException("El destino es obligatorio.");
        }

        if (string.Equals(origen, destino, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidacionException("El origen y el destino deben ser diferentes.");
        }
    }

    private static bool EsHoraValida(TimeSpan hora) =>
        hora >= TimeSpan.Zero && hora < TimeSpan.FromDays(1);

    private static string NormalizarDireccion(string direccion) =>
        direccion?.Trim() ?? string.Empty;

    private async Task<Viaje> ObtenerViajeConRelacionesAsync(
        int viajeId,
        CancellationToken cancellationToken)
    {
        var viaje = await dbContext.Viajes
            .Include(x => x.Paradas)
            .Include(x => x.Asignaciones)
                .ThenInclude(x => x.Envio)
            .SingleOrDefaultAsync(x => x.Id == viajeId, cancellationToken);

        return viaje ?? throw new NotFoundException("El viaje no existe.");
    }

    private static void ValidarAcceso(Viaje viaje, int usuarioId)
    {
        if (viaje.UsuarioId != usuarioId)
        {
            throw new AccesoDenegadoException(
                "No tenés permiso para acceder a este viaje.");
        }
    }

    private static ViajeResponse AResponse(Viaje viaje) => new()
    {
        Id = viaje.Id,
        UsuarioId = viaje.UsuarioId,
        Origen = viaje.Origen,
        Destino = viaje.Destino,
        DiasSemana = viaje.DiasSemana,
        HoraSalida = viaje.HoraSalida.ToTimeSpan(),
        HoraLlegada = viaje.HoraLlegada.ToTimeSpan(),
        MaxPaquetes = viaje.MaxPaquetes,
        PesoMaximoKg = viaje.PesoMaximoKg,
        DesvioMaximoKm = viaje.DesvioMaximoKm,
        Estado = viaje.Estado,
        Paradas = viaje.Paradas
            .OrderBy(parada => parada.Orden)
            .Select(parada => new ParadaResponse
            {
                Id = parada.Id,
                Direccion = parada.Direccion,
                Orden = parada.Orden
            })
            .ToList()
    };

    private static ViajeDetalleResponse ADetalleResponse(Viaje viaje, RutaViajeResponse ruta) => new()
    {
        Id = viaje.Id,
        UsuarioId = viaje.UsuarioId,
        Origen = viaje.Origen,
        Destino = viaje.Destino,
        DiasSemana = viaje.DiasSemana,
        HoraSalida = viaje.HoraSalida.ToTimeSpan(),
        HoraLlegada = viaje.HoraLlegada.ToTimeSpan(),
        MaxPaquetes = viaje.MaxPaquetes,
        PesoMaximoKg = viaje.PesoMaximoKg,
        DesvioMaximoKm = viaje.DesvioMaximoKm,
        Estado = viaje.Estado,
        CantidadPaquetesAsignados = viaje.Asignaciones.Count,
        PaquetesAsignados = viaje.Asignaciones
            .OrderByDescending(asignacion => asignacion.Envio.Fecha)
            .ThenBy(asignacion => asignacion.Id)
            .Select(asignacion => new PaqueteAsignadoResponse
            {
                AsignacionId = asignacion.Id,
                EnvioId = asignacion.EnvioId,
                Descripcion = asignacion.Envio.Descripcion,
                Origen = asignacion.Envio.Origen,
                Destino = asignacion.Envio.Destino,
                Fecha = asignacion.Envio.Fecha,
                PesoKg = asignacion.Envio.PesoKg,
                Estado = asignacion.Envio.Estado,
                Ganancia = asignacion.Ganancia
            })
            .ToList(),
        Ruta = ruta,
        Paradas = viaje.Paradas
            .OrderBy(parada => parada.Orden)
            .Select(parada => new ParadaResponse
            {
                Id = parada.Id,
                Direccion = parada.Direccion,
                Orden = parada.Orden
            })
            .ToList()
    };
}
