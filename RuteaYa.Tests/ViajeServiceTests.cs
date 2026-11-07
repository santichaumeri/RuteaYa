using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos;
using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Servicios;
using Xunit;

namespace RuteaYa.Tests;

public sealed class ViajeServiceTests
{
    [Fact]
    public async Task CrearViaje_guarda_el_viaje_con_dos_paradas_en_orden_secuencial()
    {
        await using var dbContext = CrearDbContext();
        var service = new ViajeService(dbContext);
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
        var service = new ViajeService(dbContext);
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

    [Fact]
    public async Task ObtenerDetalle_devuelve_datos_y_paradas_ordenadas()
    {
        await using var dbContext = CrearDbContext();
        var service = new ViajeService(dbContext);
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
        var service = new ViajeService(dbContext);
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
        var service = new ViajeService(dbContext);
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
        var service = new ViajeService(dbContext);
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
