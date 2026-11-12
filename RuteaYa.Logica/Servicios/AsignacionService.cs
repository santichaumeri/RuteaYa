using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos.Asignaciones;
using RuteaYa.Logica.Dtos.Rutas;
using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Oportunidades;
using RuteaYa.Logica.Servicios.Rutas;
using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.Logica.Servicios;

public class AsignacionService : IAsignacionService
{

    private readonly RuteaYaDbContext _dbContext;
    private readonly IEstadoEnvioService _estadoEnvioService;
    private readonly IRoutingService _routingService;
    private readonly TimeProvider _reloj;

    public AsignacionService(
        RuteaYaDbContext dbContext,
        IEstadoEnvioService estadoEnvioService,
        IRoutingService routingService,
        TimeProvider? reloj = null)
    {
        _dbContext = dbContext;
        _estadoEnvioService = estadoEnvioService;
        _routingService = routingService;
        _reloj = reloj ?? TimeProvider.System;
    }

    public async Task<AsignacionDetalleDto> AceptarOportunidadAsync(int usuarioId, AsignarOportunidadRequestDto dto)
    {
        // 1. Obtener y validar el viaje
        var viaje = await _dbContext.Viajes
            .Include(v => v.Paradas)
            .Include(v => v.Asignaciones)
                .ThenInclude(a => a.Envio)
            .FirstOrDefaultAsync(v => v.Id == dto.ViajeId);

        if (viaje == null)
            throw new KeyNotFoundException("El viaje especificado no existe.");

        if (viaje.UsuarioId != usuarioId)
            throw new UnauthorizedAccessException("No tenés permisos para gestionar este viaje.");

        if (viaje.Estado != EstadoViaje.Activo)
            throw new InvalidOperationException("No se pueden asignar envíos a un viaje inactivo.");

        // 2. Obtener y validar el envío
        var envio = await _dbContext.Envios
            .FirstOrDefaultAsync(e => e.Id == dto.EnvioId);

        if (envio == null)
            throw new KeyNotFoundException("El envío especificado no existe.");

        if (envio.UsuarioId == usuarioId)
            throw new InvalidOperationException("No podés aceptar un envío publicado por vos mismo.");

        if (envio.Estado != EstadoEnvio.BuscandoConductor)
            throw new InvalidOperationException("El envío no está disponible (ya fue asignado o cancelado).");

        // 3. Validar capacidad del viaje (paquetes y peso)
        var paquetesActuales = viaje.Asignaciones.Count;

        if (paquetesActuales + 1 > viaje.MaxPaquetes)
            throw new InvalidOperationException($"El viaje alcanzó el cupo máximo de paquetes ({viaje.MaxPaquetes}).");

        var pesoActual = viaje.Asignaciones.Sum(a => a.Envio.PesoKg);

        if (pesoActual + envio.PesoKg > viaje.PesoMaximoKg)
            throw new InvalidOperationException($"El envío excede el peso máximo permitido para este viaje ({viaje.PesoMaximoKg} kg).");

        // 4. Calcular y validar desvío
        var paradasNombres = viaje.Paradas.OrderBy(p => p.Orden).Select(p => p.Direccion).ToList();
        var calculoDesvio = await _routingService.CalcularDesvioAsync(
            viaje.Origen,
            paradasNombres,
            viaje.Destino,
            envio.Origen, 
            envio.Destino
            );

        if((decimal)calculoDesvio.DesvioKm > viaje.DesvioMaximoKm)
        {
            throw new InvalidOperationException($"El desvío resultante ({calculoDesvio.DesvioKm} km) supera el máximo tolerado ({viaje.DesvioMaximoKm} km).");
        }

        // 5. Validar transición de estado
        _estadoEnvioService.ValidarTransicion(envio.Estado, EstadoEnvio.ConductorAsignado);

        // 6.Transacción para aplicar cambios atómicos y persistir
        using var transaccion = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            envio.Estado = EstadoEnvio.ConductorAsignado;

            // Crear asignación
            var asignacion = new Asignacion
            {
                ViajeId = viaje.Id,
                EnvioId = envio.Id,
                FechaAsignacion = DateTime.UtcNow,
                DesvioKm = (decimal)calculoDesvio.DesvioKm,
                Ganancia = envio.MontoOfrecido
            };

            _dbContext.Asignaciones.Add(asignacion);

            await _dbContext.SaveChangesAsync();
            await transaccion.CommitAsync();

            return new AsignacionDetalleDto
            {
                Id = asignacion.Id,
                ViajeId = asignacion.ViajeId,
                EnvioId = asignacion.EnvioId,
                FechaAsignacion = asignacion.FechaAsignacion,
                DesvioKm = asignacion.DesvioKm,
                Ganancia = asignacion.Ganancia,
                EstadoEnvio = envio.Estado.ToString()
            };

        }
        catch (DbUpdateConcurrencyException)
        {
            await transaccion.RollbackAsync();
            throw new InvalidOperationException("El envío ya fue modificado o tomado por otro conductor en simultáneo.");
        }
        catch(DbUpdateException)
        {
            await transaccion.RollbackAsync();
            throw new InvalidOperationException("El envío ya se encuentra asignado a un viaje.");
        }
    }

    public async Task<CombinacionConfirmadaDto> ConfirmarCombinacionAsync(int usuarioId, ConfirmarCombinacionRequestDto dto)
    {
        // valido el pedido
        if (dto.EnvioIds is null || dto.EnvioIds.Count == 0)
            throw new ValidacionException("Tenés que seleccionar al menos un envío.");

        if (dto.EnvioIds.Distinct().Count() != dto.EnvioIds.Count)
            throw new ValidacionException("La combinación tiene envíos repetidos.");

        // viaje existe, es del user y esta activo
        var viaje = await _dbContext.Viajes
            .Include(v => v.Paradas)
            .FirstOrDefaultAsync(v => v.Id == dto.ViajeId)
            ?? throw new NotFoundException("El viaje especificado no existe.");

        if (viaje.UsuarioId != usuarioId)
            throw new AccesoDenegadoException("No tenés permisos para gestionar este viaje.");

        if (viaje.Estado != EstadoViaje.Activo)
            throw new ConflictException("No se pueden asignar envíos a un viaje inactivo.");

        // verifico que existan los envios
        var envios = await _dbContext.Envios
            .Where(e => dto.EnvioIds.Contains(e.Id))
            .OrderBy(e => e.Id)
            .ToListAsync();

        var inexistentes = dto.EnvioIds.Except(envios.Select(e => e.Id)).OrderBy(id => id).ToList();
        if (inexistentes.Count > 0)
            throw new NotFoundException($"No existen los envíos: {string.Join(", ", inexistentes.Select(id => $"#{id}"))}.");

        // verifico si estan todos los envios buscando conductor, sino lanzo excepcion con los que no
        var noDisponibles = envios
            .Where(e => e.Estado != EstadoEnvio.BuscandoConductor)
            .Select(e => e.Id)
            .ToList();

        if (noDisponibles.Count > 0)
            throw new CombinacionConflictoException(noDisponibles);

        // reglas por envio
        var hoy = DateOnly.FromDateTime(_reloj.GetLocalNow().DateTime);

        foreach (var envio in envios)
        {
            if (envio.UsuarioId == usuarioId)
                throw new ConflictException($"El envío #{envio.Id} lo publicaste vos y no podés llevarlo.");

            if (envio.Fecha != dto.Fecha)
                throw new ConflictException($"El envío #{envio.Id} no corresponde a la fecha {dto.Fecha:dd/MM/yyyy}.");
        }

        if (!ReglasCompatibilidad.CoincideFecha(viaje, dto.Fecha, hoy))
            throw new ConflictException($"El viaje no sale el {dto.Fecha:dd/MM/yyyy} o esa fecha ya pasó.");

        // revalidar capacidad, peso y desvío del día: lo ya asignado más la combinación completa
        var yaAsignado = await _dbContext.Asignaciones
            .AsNoTracking()
            .Where(a => a.ViajeId == viaje.Id
                && a.Envio.Fecha == dto.Fecha
                && a.Envio.Estado != EstadoEnvio.Cancelado)
            .Select(a => new { a.Envio.PesoKg, a.DesvioKm })
            .ToListAsync();

        var paradas = viaje.Paradas.OrderBy(p => p.Orden).Select(p => p.Direccion).ToList();
        var desvios = new Dictionary<int, decimal>();

        foreach (var envio in envios)
        {
            var calculo = await _routingService.CalcularDesvioAsync(
                viaje.Origen, paradas, viaje.Destino, envio.Origen, envio.Destino);

            desvios[envio.Id] = Math.Round((decimal)calculo.DesvioKm, 2);
        }

        var paquetesTotales = yaAsignado.Count + envios.Count;
        var pesoTotal = yaAsignado.Sum(a => a.PesoKg) + envios.Sum(e => e.PesoKg);
        var desvioTotal = yaAsignado.Sum(a => a.DesvioKm) + desvios.Values.Sum();

        var motivos = new List<string>();

        if (paquetesTotales > viaje.MaxPaquetes)
            motivos.Add($"Supera el cupo de paquetes del viaje: {paquetesTotales} de {viaje.MaxPaquetes}.");

        if (pesoTotal > viaje.PesoMaximoKg)
            motivos.Add($"Supera el peso máximo del viaje: {pesoTotal:0.##} kg de {viaje.PesoMaximoKg:0.##} kg.");

        if (desvioTotal > viaje.DesvioMaximoKm)
            motivos.Add($"Supera el desvío máximo del viaje: {desvioTotal:0.##} km de {viaje.DesvioMaximoKm:0.##} km.");

        if (motivos.Count > 0)
            throw new ConflictException("La combinación ya no entra en el viaje. " + string.Join(" ", motivos));

        // verifico la trans de estado de todos los envios
        foreach (var envio in envios)
            _estadoEnvioService.ValidarTransicion(envio.Estado, EstadoEnvio.ConductorAsignado);

       
        using var transaccion = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var ahora = DateTime.UtcNow;
            var asignaciones = new List<Asignacion>();

            foreach (var envio in envios)
            {
                envio.Estado = EstadoEnvio.ConductorAsignado;

                var asignacion = new Asignacion
                {
                    ViajeId = viaje.Id,
                    EnvioId = envio.Id,
                    FechaAsignacion = ahora,
                    DesvioKm = desvios[envio.Id],
                    Ganancia = envio.MontoOfrecido
                };

                _dbContext.Asignaciones.Add(asignacion);
                asignaciones.Add(asignacion);
            }

            await _dbContext.SaveChangesAsync();
            await transaccion.CommitAsync();

            return new CombinacionConfirmadaDto
            {
                ViajeId = viaje.Id,
                Fecha = dto.Fecha,
                Asignaciones = asignaciones.Select(a => new AsignacionDetalleDto
                {
                    Id = a.Id,
                    ViajeId = a.ViajeId,
                    EnvioId = a.EnvioId,
                    FechaAsignacion = a.FechaAsignacion,
                    DesvioKm = a.DesvioKm,
                    Ganancia = a.Ganancia,
                    EstadoEnvio = EstadoEnvio.ConductorAsignado.ToString()
                }).ToList(),
                CantidadPaquetes = asignaciones.Count,
                GananciaTotal = asignaciones.Sum(a => a.Ganancia),
                PesoTotalKg = envios.Sum(e => e.PesoKg),
                DesvioTotalKm = asignaciones.Sum(a => a.DesvioKm)
            };
        }
        catch (DbUpdateException)
        {
            // otro conductor tomo algo no se reservó nada.
            await transaccion.RollbackAsync();
            _dbContext.ChangeTracker.Clear();

            var perdidos = await _dbContext.Envios
                .AsNoTracking()
                .Where(e => dto.EnvioIds.Contains(e.Id)
                    && (e.Estado != EstadoEnvio.BuscandoConductor || e.Asignacion != null))
                .OrderBy(e => e.Id)
                .Select(e => e.Id)
                .ToListAsync();

            if (perdidos.Count > 0)
                throw new CombinacionConflictoException(perdidos);

            throw new ConflictException("No se pudo reservar la combinación porque otro conductor modificó alguno de los envíos. Volvé a intentar.");
        }
    }
}
