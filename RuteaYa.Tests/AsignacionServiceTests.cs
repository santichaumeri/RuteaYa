using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos.Asignaciones;
using RuteaYa.Logica.Dtos.Rutas;
using RuteaYa.Logica.Excepciones;
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

    // ---------------------------------------------------------------------------------------------
    // HU-17: confirmar una combinación completa (todo o nada)
    // ---------------------------------------------------------------------------------------------

    private static readonly DateOnly HoyCombinacion = new(2026, 11, 10);
    private static readonly DateOnly FechaCombinacion = new(2026, 11, 12);
    private const int ConductorId = 10;
    private const int RemitenteId = 20;

    private AsignacionService CrearServicioDeCombinaciones(RuteaYaDbContext context) =>
        new(context, _estadoEnvioServiceMock.Object, new RutasFalsas(), new RelojFijo(HoyCombinacion));

    /// <summary>Viaje activo que sale todos los días, con los límites indicados.</summary>
    private static Viaje CrearViajeConLimites(int maxPaquetes = 3, decimal pesoMaximoKg = 20m, decimal desvioMaximoKm = 10m) => new()
    {
        Id = 1,
        UsuarioId = ConductorId,
        Origen = "Moron",
        Destino = "Castelar",
        DiasSemana = DiasSemana.Lunes | DiasSemana.Martes | DiasSemana.Miercoles | DiasSemana.Jueves
            | DiasSemana.Viernes | DiasSemana.Sabado | DiasSemana.Domingo,
        MaxPaquetes = maxPaquetes,
        PesoMaximoKg = pesoMaximoKg,
        DesvioMaximoKm = desvioMaximoKm,
        Estado = EstadoViaje.Activo
    };

    /// <summary>El destino define el desvío (ver RutasFalsas): "Medio" 0,5 km, "A" 2 km, "Cerca" 3 km.</summary>
    private static Envio CrearEnvioParaCombinar(
        int id,
        string destino,
        decimal pesoKg,
        decimal monto,
        EstadoEnvio estado = EstadoEnvio.BuscandoConductor,
        int usuarioId = RemitenteId) => new()
    {
        Id = id,
        UsuarioId = usuarioId,
        Origen = "Moron",
        Destino = destino,
        Fecha = FechaCombinacion,
        PesoKg = pesoKg,
        MontoOfrecido = monto,
        Estado = estado
    };

    private static ConfirmarCombinacionRequestDto PedirCombinacion(params int[] envioIds) => new()
    {
        ViajeId = 1,
        Fecha = FechaCombinacion,
        EnvioIds = envioIds.ToList()
    };

    private static async Task AssertNoSeReservoNadaAsync(
        RuteaYaDbContext context,
        int asignacionesEsperadas,
        params int[] envioIdsQueSiguenLibres)
    {
        Assert.Equal(asignacionesEsperadas, await context.Asignaciones.CountAsync());

        var estados = await context.Envios
            .Where(e => envioIdsQueSiguenLibres.Contains(e.Id))
            .Select(e => e.Estado)
            .ToListAsync();

        Assert.Equal(envioIdsQueSiguenLibres.Length, estados.Count);
        Assert.All(estados, estado => Assert.Equal(EstadoEnvio.BuscandoConductor, estado));
    }

    [Fact]
    public async Task ConfirmarCombinacionAsync_DatosValidos_ReservaTodosLosEnvios()
    {
        using var context = CrearDbContextEnMemoria();
        context.Viajes.Add(CrearViajeConLimites());
        context.Envios.AddRange(
            CrearEnvioParaCombinar(100, "A", pesoKg: 4m, monto: 2500m),      // desvío 2 km
            CrearEnvioParaCombinar(101, "Cerca", pesoKg: 5m, monto: 3000m),  // desvío 3 km
            CrearEnvioParaCombinar(102, "Medio", pesoKg: 3m, monto: 1000m)); // desvío 0,5 km
        await context.SaveChangesAsync();

        var service = CrearServicioDeCombinaciones(context);

        var resultado = await service.ConfirmarCombinacionAsync(ConductorId, PedirCombinacion(100, 101, 102));

        Assert.Equal(3, resultado.CantidadPaquetes);
        Assert.Equal(6500m, resultado.GananciaTotal);
        Assert.Equal(12m, resultado.PesoTotalKg);
        Assert.Equal(5.5m, resultado.DesvioTotalKm);
        Assert.Equal(3, resultado.Asignaciones.Count);

        Assert.Equal(3, await context.Asignaciones.CountAsync());

        var estados = await context.Envios.Select(e => e.Estado).ToListAsync();
        Assert.All(estados, estado => Assert.Equal(EstadoEnvio.ConductorAsignado, estado));
    }

    [Fact]
    public async Task ConfirmarCombinacionAsync_UnEnvioYaTomado_LanzaConflictoYNoReservaNinguno()
    {
        using var context = CrearDbContextEnMemoria();
        context.Viajes.Add(CrearViajeConLimites());
        context.Envios.AddRange(
            CrearEnvioParaCombinar(100, "A", pesoKg: 4m, monto: 2500m),
            CrearEnvioParaCombinar(101, "Cerca", pesoKg: 5m, monto: 3000m),
            CrearEnvioParaCombinar(102, "Medio", pesoKg: 3m, monto: 1000m, estado: EstadoEnvio.ConductorAsignado)); // lo tomó otro
        await context.SaveChangesAsync();

        var service = CrearServicioDeCombinaciones(context);

        var ex = await Assert.ThrowsAsync<CombinacionConflictoException>(() =>
            service.ConfirmarCombinacionAsync(ConductorId, PedirCombinacion(100, 101, 102)));

        Assert.Equal(new[] { 102 }, ex.EnvioIdsNoDisponibles);

        // Todo o nada: los dos envíos libres tampoco se reservaron.
        await AssertNoSeReservoNadaAsync(context, asignacionesEsperadas: 0, 100, 101);
    }

    [Fact]
    public async Task ConfirmarCombinacionAsync_ExcedeElCupoDePaquetes_LanzaConflictoYNoReservaNada()
    {
        using var context = CrearDbContextEnMemoria();
        context.Viajes.Add(CrearViajeConLimites(maxPaquetes: 2));
        context.Envios.AddRange(
            CrearEnvioParaCombinar(100, "A", pesoKg: 1m, monto: 1000m),
            CrearEnvioParaCombinar(101, "A", pesoKg: 1m, monto: 1000m),
            CrearEnvioParaCombinar(102, "A", pesoKg: 1m, monto: 1000m));
        await context.SaveChangesAsync();

        var service = CrearServicioDeCombinaciones(context);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.ConfirmarCombinacionAsync(ConductorId, PedirCombinacion(100, 101, 102)));

        Assert.Contains("cupo de paquetes", ex.Message);
        await AssertNoSeReservoNadaAsync(context, asignacionesEsperadas: 0, 100, 101, 102);
    }

    [Fact]
    public async Task ConfirmarCombinacionAsync_ExcedeElPesoMaximo_LanzaConflictoYNoReservaNada()
    {
        using var context = CrearDbContextEnMemoria();
        context.Viajes.Add(CrearViajeConLimites(pesoMaximoKg: 10m));
        context.Envios.AddRange(
            CrearEnvioParaCombinar(100, "Medio", pesoKg: 4m, monto: 1000m),
            CrearEnvioParaCombinar(101, "Medio", pesoKg: 5m, monto: 1000m),
            CrearEnvioParaCombinar(102, "Medio", pesoKg: 3m, monto: 1000m)); // total 12 kg
        await context.SaveChangesAsync();

        var service = CrearServicioDeCombinaciones(context);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.ConfirmarCombinacionAsync(ConductorId, PedirCombinacion(100, 101, 102)));

        Assert.Contains("peso máximo", ex.Message);
        await AssertNoSeReservoNadaAsync(context, asignacionesEsperadas: 0, 100, 101, 102);
    }

    [Fact]
    public async Task ConfirmarCombinacionAsync_ExcedeElDesvioMaximo_LanzaConflictoYNoReservaNada()
    {
        using var context = CrearDbContextEnMemoria();
        context.Viajes.Add(CrearViajeConLimites(desvioMaximoKm: 4m));
        context.Envios.AddRange(
            CrearEnvioParaCombinar(100, "A", pesoKg: 1m, monto: 1000m),      // 2 km
            CrearEnvioParaCombinar(101, "Cerca", pesoKg: 1m, monto: 1000m)); // 3 km -> total 5 km
        await context.SaveChangesAsync();

        var service = CrearServicioDeCombinaciones(context);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.ConfirmarCombinacionAsync(ConductorId, PedirCombinacion(100, 101)));

        Assert.Contains("desvío máximo", ex.Message);
        await AssertNoSeReservoNadaAsync(context, asignacionesEsperadas: 0, 100, 101);
    }

    [Fact]
    public async Task ConfirmarCombinacionAsync_ConsideraLoYaAsignadoEseDia_LanzaConflicto()
    {
        using var context = CrearDbContextEnMemoria();
        context.Viajes.Add(CrearViajeConLimites(maxPaquetes: 3));
        context.Envios.AddRange(
            // Ya reservados antes para ese mismo día: ocupan 2 de los 3 lugares.
            CrearEnvioParaCombinar(200, "Medio", pesoKg: 1m, monto: 500m, estado: EstadoEnvio.ConductorAsignado),
            CrearEnvioParaCombinar(201, "Medio", pesoKg: 1m, monto: 500m, estado: EstadoEnvio.ConductorAsignado),
            // Combinación nueva: sola entraría, pero junto con lo anterior son 4 paquetes.
            CrearEnvioParaCombinar(100, "Medio", pesoKg: 1m, monto: 1000m),
            CrearEnvioParaCombinar(101, "Medio", pesoKg: 1m, monto: 1000m));
        context.Asignaciones.AddRange(
            new Asignacion { ViajeId = 1, EnvioId = 200, FechaAsignacion = DateTime.UtcNow, DesvioKm = 0.5m, Ganancia = 500m },
            new Asignacion { ViajeId = 1, EnvioId = 201, FechaAsignacion = DateTime.UtcNow, DesvioKm = 0.5m, Ganancia = 500m });
        await context.SaveChangesAsync();

        var service = CrearServicioDeCombinaciones(context);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.ConfirmarCombinacionAsync(ConductorId, PedirCombinacion(100, 101)));

        Assert.Contains("cupo de paquetes", ex.Message);
        await AssertNoSeReservoNadaAsync(context, asignacionesEsperadas: 2, 100, 101);
    }

    [Fact]
    public async Task ConfirmarCombinacionAsync_ViajeAjeno_LanzaAccesoDenegado()
    {
        using var context = CrearDbContextEnMemoria();
        context.Viajes.Add(CrearViajeConLimites());
        context.Envios.Add(CrearEnvioParaCombinar(100, "A", pesoKg: 1m, monto: 1000m));
        await context.SaveChangesAsync();

        var service = CrearServicioDeCombinaciones(context);

        await Assert.ThrowsAsync<AccesoDenegadoException>(() =>
            service.ConfirmarCombinacionAsync(usuarioId: 99, PedirCombinacion(100)));
    }

    [Fact]
    public async Task ConfirmarCombinacionAsync_EnvioPropio_LanzaConflicto()
    {
        using var context = CrearDbContextEnMemoria();
        context.Viajes.Add(CrearViajeConLimites());
        context.Envios.Add(CrearEnvioParaCombinar(100, "A", pesoKg: 1m, monto: 1000m, usuarioId: ConductorId));
        await context.SaveChangesAsync();

        var service = CrearServicioDeCombinaciones(context);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.ConfirmarCombinacionAsync(ConductorId, PedirCombinacion(100)));

        Assert.Contains("publicaste vos", ex.Message);
        await AssertNoSeReservoNadaAsync(context, asignacionesEsperadas: 0, 100);
    }

    [Fact]
    public async Task ConfirmarCombinacionAsync_SinEnvios_LanzaValidacion()
    {
        using var context = CrearDbContextEnMemoria();
        var service = CrearServicioDeCombinaciones(context);

        await Assert.ThrowsAsync<ValidacionException>(() =>
            service.ConfirmarCombinacionAsync(ConductorId, PedirCombinacion()));
    }

    [Fact]
    public async Task ConfirmarCombinacionAsync_EnviosRepetidos_LanzaValidacion()
    {
        using var context = CrearDbContextEnMemoria();
        var service = CrearServicioDeCombinaciones(context);

        await Assert.ThrowsAsync<ValidacionException>(() =>
            service.ConfirmarCombinacionAsync(ConductorId, PedirCombinacion(100, 100)));
    }
}
