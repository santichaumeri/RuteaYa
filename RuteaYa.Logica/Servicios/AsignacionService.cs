using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos.Asignaciones;
using RuteaYa.Logica.Dtos.Rutas;
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

    public AsignacionService(RuteaYaDbContext dbContext, IEstadoEnvioService estadoEnvioService, IRoutingService routingService)
    {
        _dbContext = dbContext;
        _estadoEnvioService = estadoEnvioService;
        _routingService = routingService;
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
}
