using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos.Asignaciones;
using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Servicios;
using Xunit;

namespace RuteaYa.Tests;

public class AsignacionServiceTests
{
    private readonly Mock<IEstadoEnvioService> _estadoEnvioServiceMock;

    public AsignacionServiceTests()
    {
        _estadoEnvioServiceMock = new Mock<IEstadoEnvioService>();
    }

    private RuteaYaDbContext CrearDbContextEnMemoria()
    {
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new RuteaYaDbContext(options);
    }

    // ---------------------------------------------------------------------------------------------
    // HU-16: aceptar una oportunidad individual. Usa las reglas de compatibilidad por fecha (HU-11/12).
    // ---------------------------------------------------------------------------------------------

    // Hoy es martes 10/11/2026. El viaje de estos tests sale los martes.
    private static readonly DateOnly HoyAceptar = new(2026, 11, 10);
    private static readonly DateOnly Martes17 = new(2026, 11, 17);
    private static readonly DateOnly Miercoles18 = new(2026, 11, 18);
    private static readonly DateOnly Martes24 = new(2026, 11, 24);

    private AsignacionService CrearServicioDeAceptacion(RuteaYaDbContext context)
    {
        var reloj = new RelojFijo(HoyAceptar);
        var rutas = new RutasFalsas();
        return new AsignacionService(
            context, _estadoEnvioServiceMock.Object, rutas, new OportunidadService(context, rutas, reloj), reloj);
    }

    private static Viaje CrearViajeDeLosMartes(int maxPaquetes = 3, decimal pesoMaximoKg = 15m, decimal desvioMaximoKm = 5m) => new()
    {
        Id = 1,
        UsuarioId = ConductorId,
        Origen = "Moron",
        Destino = "Castelar",
        DiasSemana = DiasSemana.Martes,
        MaxPaquetes = maxPaquetes,
        PesoMaximoKg = pesoMaximoKg,
        DesvioMaximoKm = desvioMaximoKm,
        Estado = EstadoViaje.Activo
    };

    /// <summary>El destino define el desvío (ver RutasFalsas): "Medio" 0,5 km, "A" 2 km, "Cerca" 3 km, "MuyLejos" 6 km.</summary>
    private static Envio CrearEnvioParaAceptar(
        int id,
        DateOnly fecha,
        string destino = "A",
        decimal pesoKg = 4m,
        decimal monto = 2500m,
        EstadoEnvio estado = EstadoEnvio.BuscandoConductor,
        int usuarioId = RemitenteId) => new()
    {
        Id = id,
        UsuarioId = usuarioId,
        Origen = "Moron",
        Destino = destino,
        Fecha = fecha,
        PesoKg = pesoKg,
        MontoOfrecido = monto,
        Estado = estado
    };

    private static AsignarOportunidadRequestDto Aceptar(int envioId) => new() { ViajeId = 1, EnvioId = envioId };

    /// <summary>Carga envíos ya asignados al viaje: ocupan lugar, peso y 0,5 km de desvío en su fecha.</summary>
    private static void AgregarAsignados(RuteaYaDbContext context, params Envio[] envios)
    {
        foreach (var envio in envios)
        {
            envio.Estado = EstadoEnvio.ConductorAsignado;
            context.Envios.Add(envio);
            context.Asignaciones.Add(new Asignacion
            {
                ViajeId = 1,
                EnvioId = envio.Id,
                FechaAsignacion = DateTime.UtcNow,
                DesvioKm = 0.5m,
                Ganancia = envio.MontoOfrecido
            });
        }
    }

    [Fact]
    public async Task AceptarOportunidadAsync_DatosValidos_AsignaCorrectamente()
    {
        using var context = CrearDbContextEnMemoria();
        context.Viajes.Add(CrearViajeDeLosMartes());
        context.Envios.Add(CrearEnvioParaAceptar(100, Martes17, destino: "A", pesoKg: 4m, monto: 2500m));
        await context.SaveChangesAsync();

        var resultado = await CrearServicioDeAceptacion(context).AceptarOportunidadAsync(ConductorId, Aceptar(100));

        Assert.Equal(1, resultado.ViajeId);
        Assert.Equal(100, resultado.EnvioId);
        Assert.Equal(2.0m, resultado.DesvioKm);
        Assert.Equal(2500m, resultado.Ganancia);
        Assert.Equal(EstadoEnvio.ConductorAsignado, (await context.Envios.FindAsync(100))!.Estado);
        Assert.NotNull(await context.Asignaciones.SingleOrDefaultAsync(a => a.EnvioId == 100));
    }

    [Fact]
    public async Task AceptarOportunidadAsync_ViajeRecurrenteLlenoUnMartes_PermiteAceptarParaElMartesSiguiente()
    {
        using var context = CrearDbContextEnMemoria();
        context.Viajes.Add(CrearViajeDeLosMartes(maxPaquetes: 2, pesoMaximoKg: 10m));
        // El martes 17 ya está completo: 2 de 2 paquetes y 10 de 10 kg.
        AgregarAsignados(context,
            CrearEnvioParaAceptar(200, Martes17, destino: "Medio", pesoKg: 5m),
            CrearEnvioParaAceptar(201, Martes17, destino: "Medio", pesoKg: 5m));
        context.Envios.Add(CrearEnvioParaAceptar(100, Martes24, pesoKg: 5m));
        await context.SaveChangesAsync();

        var resultado = await CrearServicioDeAceptacion(context).AceptarOportunidadAsync(ConductorId, Aceptar(100));

        // La capacidad es por fecha: el martes 24 el viaje sale vacío.
        Assert.Equal(100, resultado.EnvioId);
        Assert.Equal(3, await context.Asignaciones.CountAsync());
    }

    [Fact]
    public async Task AceptarOportunidadAsync_CupoDelMismoDiaCompleto_LanzaConflicto()
    {
        using var context = CrearDbContextEnMemoria();
        context.Viajes.Add(CrearViajeDeLosMartes(maxPaquetes: 2));
        AgregarAsignados(context,
            CrearEnvioParaAceptar(200, Martes17, destino: "Medio", pesoKg: 1m),
            CrearEnvioParaAceptar(201, Martes17, destino: "Medio", pesoKg: 1m));
        context.Envios.Add(CrearEnvioParaAceptar(100, Martes17, pesoKg: 1m));
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            CrearServicioDeAceptacion(context).AceptarOportunidadAsync(ConductorId, Aceptar(100)));

        Assert.Contains("cantidad máxima de paquetes", ex.Message);
        Assert.Equal(EstadoEnvio.BuscandoConductor, (await context.Envios.FindAsync(100))!.Estado);
        Assert.Equal(2, await context.Asignaciones.CountAsync());
    }

    [Fact]
    public async Task AceptarOportunidadAsync_PesoDelMismoDiaSuperado_LanzaConflicto()
    {
        using var context = CrearDbContextEnMemoria();
        context.Viajes.Add(CrearViajeDeLosMartes(pesoMaximoKg: 10m));
        AgregarAsignados(context, CrearEnvioParaAceptar(200, Martes17, destino: "Medio", pesoKg: 8m));
        context.Envios.Add(CrearEnvioParaAceptar(100, Martes17, pesoKg: 3m)); // 8 + 3 = 11 kg
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            CrearServicioDeAceptacion(context).AceptarOportunidadAsync(ConductorId, Aceptar(100)));

        Assert.Contains("peso disponible", ex.Message);
    }

    [Fact]
    public async Task AceptarOportunidadAsync_DesvioAcumuladoDelDiaSuperado_LanzaConflicto()
    {
        using var context = CrearDbContextEnMemoria();
        context.Viajes.Add(CrearViajeDeLosMartes(desvioMaximoKm: 3m));
        // Ya hay un envío ese día con 0,5 km de desvío: quedan 2,5 km y el nuevo agrega 3 km.
        // Solo, entraría en los 3 km del viaje; sumado a lo del día, no.
        AgregarAsignados(context, CrearEnvioParaAceptar(200, Martes17, destino: "Medio", pesoKg: 1m));
        context.Envios.Add(CrearEnvioParaAceptar(100, Martes17, destino: "Cerca", pesoKg: 1m));
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            CrearServicioDeAceptacion(context).AceptarOportunidadAsync(ConductorId, Aceptar(100)));

        Assert.Contains("desvío disponible", ex.Message);
    }

    [Fact]
    public async Task AceptarOportunidadAsync_DesvioSuperaMaximoTolerado_LanzaConflicto()
    {
        using var context = CrearDbContextEnMemoria();
        context.Viajes.Add(CrearViajeDeLosMartes(desvioMaximoKm: 3m));
        context.Envios.Add(CrearEnvioParaAceptar(100, Martes17, destino: "MuyLejos")); // 6 km
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            CrearServicioDeAceptacion(context).AceptarOportunidadAsync(ConductorId, Aceptar(100)));

        Assert.Contains("desvío disponible", ex.Message);
    }

    [Fact]
    public async Task AceptarOportunidadAsync_ElViajeNoSaleEseDia_LanzaConflicto()
    {
        using var context = CrearDbContextEnMemoria();
        context.Viajes.Add(CrearViajeDeLosMartes());
        context.Envios.Add(CrearEnvioParaAceptar(100, Miercoles18));
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            CrearServicioDeAceptacion(context).AceptarOportunidadAsync(ConductorId, Aceptar(100)));

        Assert.Contains("no sale", ex.Message);
    }

    [Fact]
    public async Task AceptarOportunidadAsync_ViajeAjeno_LanzaAccesoDenegado()
    {
        using var context = CrearDbContextEnMemoria();
        context.Viajes.Add(CrearViajeDeLosMartes());
        context.Envios.Add(CrearEnvioParaAceptar(100, Martes17));
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<AccesoDenegadoException>(() =>
            CrearServicioDeAceptacion(context).AceptarOportunidadAsync(usuarioId: 99, Aceptar(100)));
    }

    [Fact]
    public async Task AceptarOportunidadAsync_EnvioInexistente_LanzaNotFound()
    {
        using var context = CrearDbContextEnMemoria();
        context.Viajes.Add(CrearViajeDeLosMartes());
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CrearServicioDeAceptacion(context).AceptarOportunidadAsync(ConductorId, Aceptar(999)));
    }

    [Fact]
    public async Task AceptarOportunidadAsync_MismoUsuarioRemitenteYConductor_LanzaConflicto()
    {
        using var context = CrearDbContextEnMemoria();
        context.Viajes.Add(CrearViajeDeLosMartes());
        context.Envios.Add(CrearEnvioParaAceptar(100, Martes17, usuarioId: ConductorId));
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            CrearServicioDeAceptacion(context).AceptarOportunidadAsync(ConductorId, Aceptar(100)));

        Assert.Contains("publicado por vos", ex.Message);
    }

    [Fact]
    public async Task AceptarOportunidadAsync_EnvioNoDisponible_LanzaConflicto()
    {
        using var context = CrearDbContextEnMemoria();
        context.Viajes.Add(CrearViajeDeLosMartes());
        context.Envios.Add(CrearEnvioParaAceptar(100, Martes17, estado: EstadoEnvio.ConductorAsignado));
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            CrearServicioDeAceptacion(context).AceptarOportunidadAsync(ConductorId, Aceptar(100)));

        Assert.Contains("ya no está disponible", ex.Message);
    }

    [Fact]
    public async Task AceptarOportunidadAsync_ViajeInactivo_LanzaConflicto()
    {
        using var context = CrearDbContextEnMemoria();
        var viaje = CrearViajeDeLosMartes();
        viaje.Estado = EstadoViaje.Inactivo;
        context.Viajes.Add(viaje);
        context.Envios.Add(CrearEnvioParaAceptar(100, Martes17));
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            CrearServicioDeAceptacion(context).AceptarOportunidadAsync(ConductorId, Aceptar(100)));

        Assert.Contains("inactivo", ex.Message);
    }

    // ---------------------------------------------------------------------------------------------
    // HU-17: confirmar una combinación completa (todo o nada)
    // ---------------------------------------------------------------------------------------------

    private static readonly DateOnly HoyCombinacion = new(2026, 11, 10);
    private static readonly DateOnly FechaCombinacion = new(2026, 11, 12);
    private const int ConductorId = 10;
    private const int RemitenteId = 20;

    private AsignacionService CrearServicioDeCombinaciones(RuteaYaDbContext context)
    {
        var reloj = new RelojFijo(HoyCombinacion);
        var rutas = new RutasFalsas();
        return new(context, _estadoEnvioServiceMock.Object, rutas, new OportunidadService(context, rutas, reloj), reloj);
    }

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
