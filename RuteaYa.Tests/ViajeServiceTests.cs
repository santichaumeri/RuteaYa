using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos;
using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Servicios;
using RuteaYa.Logica.Servicios.Rutas;
using Xunit;

namespace RuteaYa.Tests;

public sealed class ViajeServiceTests
{
    [Fact]
    public async Task ObtenerMisViajes_filtra_por_estado_y_aisla_por_usuario()
    {
        await using var dbContext = CrearDbContext();
        dbContext.Viajes.AddRange(
            new Viaje { Id = 1, UsuarioId = 1, Origen = "A", Destino = "B", Estado = EstadoViaje.Activo },
            new Viaje { Id = 2, UsuarioId = 1, Origen = "C", Destino = "D", Estado = EstadoViaje.Inactivo },
            new Viaje { Id = 3, UsuarioId = 2, Origen = "E", Destino = "F", Estado = EstadoViaje.Activo });
        await dbContext.SaveChangesAsync();

        var service = new ViajeService(dbContext, new MockRoutingService());

        var activos = await service.ObtenerMisViajesAsync(1, EstadoViaje.Activo, CancellationToken.None);
        var todos = await service.ObtenerMisViajesAsync(1, null, CancellationToken.None);

        var viajeActivo = Assert.Single(activos);
        Assert.Equal(1, viajeActivo.Id);
        Assert.Equal(2, todos.Count);
        Assert.DoesNotContain(todos, viaje => viaje.UsuarioId != 1);
    }

    [Fact]
    public async Task DesactivarViaje_cambia_el_estado_si_pertenece_al_usuario()
    {
        await using var dbContext = CrearDbContext();
        dbContext.Viajes.Add(new Viaje { Id = 1, UsuarioId = 1, Estado = EstadoViaje.Activo });
        await dbContext.SaveChangesAsync();

        await new ViajeService(dbContext, new MockRoutingService()).DesactivarViajeAsync(1, 1, CancellationToken.None);

        Assert.Equal(EstadoViaje.Inactivo, (await dbContext.Viajes.SingleAsync()).Estado);
    }

    [Fact]
    public async Task DesactivarViaje_rechaza_viaje_ajeno_y_viaje_ya_inactivo()
    {
        await using var dbContext = CrearDbContext();
        dbContext.Viajes.AddRange(
            new Viaje { Id = 1, UsuarioId = 1, Estado = EstadoViaje.Activo },
            new Viaje { Id = 2, UsuarioId = 1, Estado = EstadoViaje.Inactivo });
        await dbContext.SaveChangesAsync();
        var service = new ViajeService(dbContext, new MockRoutingService());

        await Assert.ThrowsAsync<NotFoundException>(() => service.DesactivarViajeAsync(2, 1, CancellationToken.None));
        await Assert.ThrowsAsync<ConflictException>(() => service.DesactivarViajeAsync(1, 2, CancellationToken.None));
    }

    [Fact]
    public async Task CrearViaje_guarda_el_viaje_con_dos_paradas_en_orden_secuencial()
    {
        await using var dbContext = CrearDbContext();
        var service = new ViajeService(dbContext, new MockRoutingService());
        var request = new CrearViajeRequest
        {
            Origen = "Belgrano",
            Destino = "Lomas de Zamora",
            DiasSemana = DiasSemana.Lunes | DiasSemana.Miercoles,
            HoraSalida = new TimeSpan(8, 0, 0),
            HoraLlegada = new TimeSpan(9, 0, 0),
            MaxPaquetes = 4,
            PesoMaximoKg = 25,
            DesvioMaximoKm = 8,
            Paradas =
            [
                new CrearParadaRequest { Direccion = "Palermo" },
                new CrearParadaRequest { Direccion = "Almagro" }
            ]
        };

        var response = await service.CrearViajeAsync(7, request, CancellationToken.None);

        Assert.NotEqual(0, response.Id);
        Assert.Equal(7, response.UsuarioId);
        Assert.Equal(EstadoViaje.Activo, response.Estado);
        Assert.Equal(2, response.Paradas.Count);
        Assert.Collection(
            response.Paradas,
            parada =>
            {
                Assert.Equal(1, parada.Orden);
                Assert.Equal("Palermo", parada.Direccion);
            },
            parada =>
            {
                Assert.Equal(2, parada.Orden);
                Assert.Equal("Almagro", parada.Direccion);
            });

        var viaje = await dbContext.Viajes
            .Include(x => x.Paradas)
            .SingleAsync();

        Assert.Equal(7, viaje.UsuarioId);
        Assert.Equal(EstadoViaje.Activo, viaje.Estado);
        Assert.Equal(2, viaje.Paradas.Count);
    }

    [Fact]
    public async Task CrearViaje_rechaza_origen_igual_destino_y_dias_sin_seleccionar()
    {
        await using var dbContext = CrearDbContext();
        var service = new ViajeService(dbContext, new MockRoutingService());
        var request = CrearRequestValida();
        request.Origen = "Rosario";
        request.Destino = " rosario ";

        await Assert.ThrowsAsync<ValidacionException>(() =>
            service.CrearViajeAsync(1, request, CancellationToken.None));

        request = CrearRequestValida();
        request.DiasSemana = DiasSemana.Ninguno;

        await Assert.ThrowsAsync<ValidacionException>(() =>
            service.CrearViajeAsync(1, request, CancellationToken.None));
    }

    [Theory]
    [InlineData(9, 0, 8, 0)]   // llega antes de salir
    [InlineData(8, 0, 8, 0)]   // llega a la misma hora en que sale
    [InlineData(23, 0, 1, 0)]  // cruza la medianoche
    public async Task CrearViaje_rechaza_llegada_que_no_es_posterior_a_la_salida(
        int horaSalida, int minutoSalida, int horaLlegada, int minutoLlegada)
    {
        await using var dbContext = CrearDbContext();
        var service = new ViajeService(dbContext, new MockRoutingService());
        var request = CrearRequestValida();
        request.HoraSalida = new TimeSpan(horaSalida, minutoSalida, 0);
        request.HoraLlegada = new TimeSpan(horaLlegada, minutoLlegada, 0);

        var ex = await Assert.ThrowsAsync<ValidacionException>(() =>
            service.CrearViajeAsync(1, request, CancellationToken.None));

        Assert.Equal("La hora de llegada tiene que ser posterior a la hora de salida.", ex.Message);
        Assert.Empty(dbContext.Viajes);
    }

    [Fact]
    public async Task EditarViaje_rechaza_llegada_anterior_a_la_salida()
    {
        await using var dbContext = CrearDbContext();
        var service = new ViajeService(dbContext, new MockRoutingService());
        var creado = await service.CrearViajeAsync(7, CrearRequestValida(), CancellationToken.None);

        var request = new EditarViajeRequest
        {
            Origen = "Belgrano",
            Destino = "Lomas de Zamora",
            DiasSemana = DiasSemana.Lunes,
            HoraSalida = new TimeSpan(18, 0, 0),
            HoraLlegada = new TimeSpan(17, 30, 0),
            MaxPaquetes = 1,
            PesoMaximoKg = 1,
            DesvioMaximoKm = 1
        };

        var ex = await Assert.ThrowsAsync<ValidacionException>(() =>
            service.EditarViajeAsync(creado.Id, 7, request, CancellationToken.None));

        Assert.Contains("posterior a la hora de salida", ex.Message);
        var guardado = await dbContext.Viajes.SingleAsync();
        Assert.Equal(new TimeOnly(8, 0), guardado.HoraSalida);
        Assert.Equal(new TimeOnly(9, 0), guardado.HoraLlegada);
    }

    [Fact]
    public async Task ObtenerDetalle_devuelve_datos_y_paradas_ordenadas()
    {
        await using var dbContext = CrearDbContext();
        var service = new ViajeService(dbContext, new MockRoutingService());
        var creado = await service.CrearViajeAsync(7, new CrearViajeRequest
        {
            Origen = "Belgrano",
            Destino = "Lomas de Zamora",
            DiasSemana = DiasSemana.Lunes | DiasSemana.Viernes,
            HoraSalida = new TimeSpan(8, 30, 0),
            HoraLlegada = new TimeSpan(10, 0, 0),
            MaxPaquetes = 4,
            PesoMaximoKg = 25,
            DesvioMaximoKm = 8,
            Paradas =
            [
                new CrearParadaRequest { Direccion = "Palermo" },
                new CrearParadaRequest { Direccion = "Almagro" }
            ]
        }, CancellationToken.None);

        var detalle = await service.ObtenerDetalleAsync(creado.Id, 7, CancellationToken.None);

        Assert.Equal(creado.Id, detalle.Id);
        Assert.Equal(7, detalle.UsuarioId);
        Assert.Equal(DiasSemana.Lunes | DiasSemana.Viernes, detalle.DiasSemana);
        Assert.Equal(new TimeSpan(8, 30, 0), detalle.HoraSalida);
        Assert.Equal(2, detalle.Paradas.Count);
        Assert.Collection(
            detalle.Paradas,
            parada =>
            {
                Assert.Equal(1, parada.Orden);
                Assert.Equal("Palermo", parada.Direccion);
            },
            parada =>
            {
                Assert.Equal(2, parada.Orden);
                Assert.Equal("Almagro", parada.Direccion);
            });
    }

    [Fact]
    public async Task ObtenerDetalle_rechaza_acceso_de_otro_usuario()
    {
        await using var dbContext = CrearDbContext();
        var service = new ViajeService(dbContext, new MockRoutingService());
        var creado = await service.CrearViajeAsync(
            7,
            CrearRequestValida(),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<AccesoDenegadoException>(() =>
            service.ObtenerDetalleAsync(creado.Id, 99, CancellationToken.None));

        Assert.Equal("No tenés permiso para acceder a este viaje.", exception.Message);
    }

    [Fact]
    public async Task EditarViaje_actualiza_datos_y_reemplaza_paradas_en_orden()
    {
        await using var dbContext = CrearDbContext();
        var service = new ViajeService(dbContext, new MockRoutingService());
        var creado = await service.CrearViajeAsync(
            7,
            new CrearViajeRequest
            {
                Origen = "Belgrano",
                Destino = "Lomas de Zamora",
                DiasSemana = DiasSemana.Lunes,
                HoraSalida = new TimeSpan(8, 0, 0),
                HoraLlegada = new TimeSpan(9, 0, 0),
                MaxPaquetes = 2,
                PesoMaximoKg = 10,
                DesvioMaximoKm = 5,
                Paradas = [new CrearParadaRequest { Direccion = "Palermo" }]
            },
            CancellationToken.None);

        var editado = await service.EditarViajeAsync(
            creado.Id,
            7,
            new EditarViajeRequest
            {
                Origen = "Caballito",
                Destino = "Temperley",
                DiasSemana = DiasSemana.Martes | DiasSemana.Jueves,
                HoraSalida = new TimeSpan(7, 15, 0),
                HoraLlegada = new TimeSpan(8, 45, 0),
                MaxPaquetes = 6,
                PesoMaximoKg = 30,
                DesvioMaximoKm = 12,
                Paradas =
                [
                    new CrearParadaRequest { Direccion = "Flores" },
                    new CrearParadaRequest { Direccion = "Boedo" }
                ]
            },
            CancellationToken.None);

        Assert.Equal("Caballito", editado.Origen);
        Assert.Equal("Temperley", editado.Destino);
        Assert.Equal(DiasSemana.Martes | DiasSemana.Jueves, editado.DiasSemana);
        Assert.Equal(6, editado.MaxPaquetes);
        Assert.Equal(2, editado.Paradas.Count);
        Assert.Collection(
            editado.Paradas,
            parada =>
            {
                Assert.Equal(1, parada.Orden);
                Assert.Equal("Flores", parada.Direccion);
            },
            parada =>
            {
                Assert.Equal(2, parada.Orden);
                Assert.Equal("Boedo", parada.Direccion);
            });

        var paradasEnDb = await dbContext.Paradas
            .Where(parada => parada.ViajeId == creado.Id)
            .OrderBy(parada => parada.Orden)
            .ToListAsync();

        Assert.Equal(2, paradasEnDb.Count);
        Assert.DoesNotContain(paradasEnDb, parada => parada.Direccion == "Palermo");
    }

    [Fact]
    public async Task EditarViaje_rechaza_datos_invalidos()
    {
        await using var dbContext = CrearDbContext();
        var service = new ViajeService(dbContext, new MockRoutingService());
        var creado = await service.CrearViajeAsync(
            7,
            CrearRequestValida(),
            CancellationToken.None);

        var request = new EditarViajeRequest
        {
            Origen = "Caballito",
            Destino = "Caballito",
            DiasSemana = DiasSemana.Lunes,
            HoraSalida = new TimeSpan(8, 0, 0),
            HoraLlegada = new TimeSpan(9, 0, 0),
            MaxPaquetes = 1,
            PesoMaximoKg = 1,
            DesvioMaximoKm = 1
        };

        await Assert.ThrowsAsync<ValidacionException>(() =>
            service.EditarViajeAsync(creado.Id, 7, request, CancellationToken.None));
    }

    [Fact]
    public async Task CalcularRuta_suma_los_tramos_en_el_orden_de_las_paradas()
    {
        await using var dbContext = CrearDbContext();
        var service = new ViajeService(dbContext, new MockRoutingService());

        // Distancias conocidas del mock: Moron-Haedo 3 km y Haedo-CABA 19 km. 1.5 min por km.
        var ruta = await service.CalcularRutaAsync(
            new RutaViajeRequest { Origen = " Moron ", Destino = "CABA", Paradas = [" Haedo "] },
            CancellationToken.None);

        Assert.Equal(22m, ruta.DistanciaKm);
        Assert.Equal(33, ruta.DuracionMinutos);
        Assert.Equal(["Moron", "Haedo", "CABA"], ruta.Puntos);
    }

    [Theory]
    [InlineData("", "CABA", new string[0], "El origen es obligatorio.")]
    [InlineData("Moron", "moron", new string[0], "El origen y el destino deben ser diferentes.")]
    [InlineData("Moron", "CABA", new[] { "Haedo", "haedo" }, "No se pueden repetir direcciones de paradas.")]
    [InlineData("Moron", "CABA", new[] { "CABA" }, "Una parada no puede coincidir con el origen ni con el destino.")]
    public async Task CalcularRuta_valida_el_recorrido_con_las_reglas_del_alta(
        string origen, string destino, string[] paradas, string mensaje)
    {
        await using var dbContext = CrearDbContext();
        var service = new ViajeService(dbContext, new MockRoutingService());

        var error = await Assert.ThrowsAsync<ValidacionException>(() => service.CalcularRutaAsync(
            new RutaViajeRequest { Origen = origen, Destino = destino, Paradas = [.. paradas] },
            CancellationToken.None));

        Assert.Equal(mensaje, error.Message);
    }

    [Fact]
    public async Task ObtenerDetalle_incluye_la_ruta_con_las_paradas_guardadas()
    {
        await using var dbContext = CrearDbContext();
        var service = new ViajeService(dbContext, new MockRoutingService());
        var request = CrearRequestValida();
        request.Origen = "Moron";
        request.Destino = "CABA";
        request.Paradas = [new CrearParadaRequest { Direccion = "Haedo" }];
        var creado = await service.CrearViajeAsync(7, request, CancellationToken.None);

        var detalle = await service.ObtenerDetalleAsync(creado.Id, 7, CancellationToken.None);

        Assert.NotNull(detalle.Ruta);
        Assert.Equal(22m, detalle.Ruta.DistanciaKm);
        Assert.Equal(["Moron", "Haedo", "CABA"], detalle.Ruta.Puntos);
    }

    [Fact]
    public async Task ObtenerDetalle_lista_los_paquetes_asignados_del_mas_reciente_al_mas_antiguo()
    {
        await using var dbContext = CrearDbContext();
        var service = new ViajeService(dbContext, new MockRoutingService());
        var creado = await service.CrearViajeAsync(7, CrearRequestValida(), CancellationToken.None);
        dbContext.Asignaciones.AddRange(
            new Asignacion
            {
                ViajeId = creado.Id,
                Ganancia = 3000m,
                Envio = new Envio { UsuarioId = 2, Origen = "Haedo", Destino = "CABA", Descripcion = "Caja", PesoKg = 2, Fecha = new DateOnly(2026, 11, 2), Estado = EstadoEnvio.Entregado }
            },
            new Asignacion
            {
                ViajeId = creado.Id,
                Ganancia = 4500m,
                Envio = new Envio { UsuarioId = 3, Origen = "Moron", Destino = "Flores", Descripcion = "Notebook", PesoKg = 3, Fecha = new DateOnly(2026, 11, 23), Estado = EstadoEnvio.ConductorAsignado }
            });
        await dbContext.SaveChangesAsync();

        var detalle = await service.ObtenerDetalleAsync(creado.Id, 7, CancellationToken.None);

        Assert.Equal(2, detalle.CantidadPaquetesAsignados);
        Assert.Equal(["Notebook", "Caja"], detalle.PaquetesAsignados.Select(x => x.Descripcion));
        var notebook = detalle.PaquetesAsignados[0];
        Assert.Equal(EstadoEnvio.ConductorAsignado, notebook.Estado);
        Assert.Equal(4500m, notebook.Ganancia);
        Assert.Equal("Moron", notebook.Origen);
        Assert.Equal("Flores", notebook.Destino);
        Assert.Equal(new DateOnly(2026, 11, 23), notebook.Fecha);
    }

    private static CrearViajeRequest CrearRequestValida() => new()
    {
        Origen = "Belgrano",
        Destino = "Lomas de Zamora",
        DiasSemana = DiasSemana.Lunes,
        HoraSalida = new TimeSpan(8, 0, 0),
        HoraLlegada = new TimeSpan(9, 0, 0),
        MaxPaquetes = 1,
        PesoMaximoKg = 1,
        DesvioMaximoKm = 1
    };

    private static RuteaYaDbContext CrearDbContext()
    {
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new RuteaYaDbContext(options);
    }
}
