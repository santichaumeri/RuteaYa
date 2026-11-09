using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos.Asignaciones;
using RuteaYa.Logica.Dtos.Rutas;
using RuteaYa.Logica.Servicios;
using RuteaYa.Logica.Servicios.Rutas;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace RuteaYa.Tests;

public class AsignacionServiceTests
{
    private readonly Mock<IEstadoEnvioService> _estadoEnvioServiceMock;
    private readonly Mock<IRoutingService> _routingServiceMock;

    public AsignacionServiceTests()
    {
        _estadoEnvioServiceMock = new Mock<IEstadoEnvioService>();
        _routingServiceMock = new Mock<IRoutingService>();
    }

    private RuteaYaDbContext CrearDbContextEnMemoria()
    {
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new RuteaYaDbContext(options);
    }

    [Fact]
    public async Task AceptarOportunidadAsync_DatosValidos_AsignaCorrectamente()
    {
        
        using var context = CrearDbContextEnMemoria();
        var conductorId = 10;
        var remitenteId = 20;

        var viaje = new Viaje
        {
            Id = 1,
            UsuarioId = conductorId,
            Origen = "Moron",
            Destino = "Castelar",
            MaxPaquetes = 3,
            PesoMaximoKg = 15m,
            DesvioMaximoKm = 5m,
            Estado = EstadoViaje.Activo
        };

        var envio = new Envio
        {
            Id = 100,
            UsuarioId = remitenteId,
            Origen = "Moron",
            Destino = "Haedo",
            PesoKg = 4m,
            MontoOfrecido = 2500m,
            Estado = EstadoEnvio.BuscandoConductor
        };

        context.Viajes.Add(viaje);
        context.Envios.Add(envio);
        await context.SaveChangesAsync();

        _routingServiceMock
            .Setup(r => r.CalcularDesvioAsync(viaje.Origen, It.IsAny<List<string>>(), viaje.Destino, envio.Origen, envio.Destino))
            .ReturnsAsync(new CalculoDesvioDto
            {
                DistanciaHabitualKm = 2.5,
                DistanciaConEnvioKm = 4.5,
                DuracionHabitual = TimeSpan.FromMinutes(10),
                DuracionConEnvio = TimeSpan.FromMinutes(13)
            });

        var service = new AsignacionService(context, _estadoEnvioServiceMock.Object, _routingServiceMock.Object);
        var request = new AsignarOportunidadRequestDto { ViajeId = viaje.Id, EnvioId = envio.Id };

        
        var resultado = await service.AceptarOportunidadAsync(conductorId, request);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(viaje.Id, resultado.ViajeId);
        Assert.Equal(envio.Id, resultado.EnvioId);
        Assert.Equal(2.0m, resultado.DesvioKm);
        Assert.Equal(2500m, resultado.Ganancia);

        var envioActualizado = await context.Envios.FindAsync(envio.Id);
        Assert.Equal(EstadoEnvio.ConductorAsignado, envioActualizado!.Estado);

        var asignacionPersistida = await context.Asignaciones.FirstOrDefaultAsync(a => a.EnvioId == envio.Id);
        Assert.NotNull(asignacionPersistida);
    }

    [Fact]
    public async Task AceptarOportunidadAsync_ViajeAjeno_LanzaUnauthorizedAccessException()
    {
        
        using var context = CrearDbContextEnMemoria();
        var propietarioViajeId = 10;
        var usuarioIntrusoId = 99;

        var viaje = new Viaje
        {
            Id = 1,
            UsuarioId = propietarioViajeId,
            Origen = "Moron",
            Destino = "Castelar",
            Estado = EstadoViaje.Activo
        };

        var envio = new Envio
        {
            Id = 100,
            UsuarioId = 20,
            Origen = "Moron",
            Destino = "Castelar",
            Estado = EstadoEnvio.BuscandoConductor
        };

        context.Viajes.Add(viaje);
        context.Envios.Add(envio);
        await context.SaveChangesAsync();

        var service = new AsignacionService(context, _estadoEnvioServiceMock.Object, _routingServiceMock.Object);

        
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.AceptarOportunidadAsync(usuarioIntrusoId, new AsignarOportunidadRequestDto { ViajeId = 1, EnvioId = 100 }));
    }

    [Fact]
    public async Task AceptarOportunidadAsync_MismoUsuarioRemitenteYConductor_LanzaInvalidOperationException()
    {
        
        using var context = CrearDbContextEnMemoria();
        var usuarioId = 10;

        var viaje = new Viaje
        {
            Id = 1,
            UsuarioId = usuarioId,
            Origen = "Moron",
            Destino = "Castelar",
            Estado = EstadoViaje.Activo
        };

        var envioPropio = new Envio
        {
            Id = 100,
            UsuarioId = usuarioId, // Mismo usuario
            Origen = "Moron",
            Destino = "Castelar",
            Estado = EstadoEnvio.BuscandoConductor
        };

        context.Viajes.Add(viaje);
        context.Envios.Add(envioPropio);
        await context.SaveChangesAsync();

        var service = new AsignacionService(context, _estadoEnvioServiceMock.Object, _routingServiceMock.Object);

        
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AceptarOportunidadAsync(usuarioId, new AsignarOportunidadRequestDto { ViajeId = 1, EnvioId = 100 }));

        Assert.Contains("publicado por vos mismo", ex.Message);
    }

    [Fact]
    public async Task AceptarOportunidadAsync_EnvioNoDisponible_LanzaInvalidOperationException()
    {
        
        using var context = CrearDbContextEnMemoria();
        var conductorId = 10;

        var viaje = new Viaje
        {
            Id = 1,
            UsuarioId = conductorId,
            Origen = "Moron",
            Destino = "Castelar",
            Estado = EstadoViaje.Activo
        };

        var envioTomado = new Envio
        {
            Id = 100,
            UsuarioId = 20,
            Origen = "Moron",
            Destino = "Castelar",
            Estado = EstadoEnvio.ConductorAsignado // Ya asignado
        };

        context.Viajes.Add(viaje);
        context.Envios.Add(envioTomado);
        await context.SaveChangesAsync();

        var service = new AsignacionService(context, _estadoEnvioServiceMock.Object, _routingServiceMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AceptarOportunidadAsync(conductorId, new AsignarOportunidadRequestDto { ViajeId = 1, EnvioId = 100 }));

        Assert.Contains("no está disponible", ex.Message);
    }

    [Fact]
    public async Task AceptarOportunidadAsync_DesvioSuperaMaximoTolerado_LanzaInvalidOperationException()
    {
        
        using var context = CrearDbContextEnMemoria();
        var conductorId = 10;

        var viaje = new Viaje
        {
            Id = 1,
            UsuarioId = conductorId,
            Origen = "Moron",
            Destino = "Castelar",
            MaxPaquetes = 5,
            PesoMaximoKg = 50m,
            DesvioMaximoKm = 3m, // Máximo 3 km
            Estado = EstadoViaje.Activo
        };

        var envio = new Envio
        {
            Id = 100,
            UsuarioId = 20,
            Origen = "Moron",
            Destino = "Lujan",
            PesoKg = 2m,
            MontoOfrecido = 1000m,
            Estado = EstadoEnvio.BuscandoConductor
        };

        context.Viajes.Add(viaje);
        context.Envios.Add(envio);
        await context.SaveChangesAsync();

        _routingServiceMock
            .Setup(r => r.CalcularDesvioAsync(viaje.Origen, It.IsAny<List<string>>(), viaje.Destino, envio.Origen, envio.Destino))
            .ReturnsAsync(new CalculoDesvioDto
            {
                DistanciaHabitualKm = 2.5,
                DistanciaConEnvioKm = 10.0,
            });

        var service = new AsignacionService(context, _estadoEnvioServiceMock.Object, _routingServiceMock.Object);

        
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AceptarOportunidadAsync(conductorId, new AsignarOportunidadRequestDto { ViajeId = 1, EnvioId = 100 }));

        Assert.Contains("supera el máximo tolerado", ex.Message);
    }
}
