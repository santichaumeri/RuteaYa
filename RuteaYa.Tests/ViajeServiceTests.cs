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
