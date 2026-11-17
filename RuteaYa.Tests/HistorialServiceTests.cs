using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos.Historial;
using RuteaYa.Logica.Servicios;
using Xunit;

namespace RuteaYa.Tests;

public sealed class HistorialServiceTests
{
    [Fact]
    public async Task Historial_calcula_totales_solo_con_envios_entregados()
    {
        await using var db = CrearDb();
        db.Viajes.AddRange(
            new Viaje { Id = 1, UsuarioId = 1, Origen = "A", Destino = "B" },
            new Viaje { Id = 2, UsuarioId = 1, Origen = "C", Destino = "D" });
        db.Envios.AddRange(
            new Envio { Id = 1, Origen = "A", Destino = "B", Fecha = new DateOnly(2026, 1, 2), Estado = EstadoEnvio.Entregado },
            new Envio { Id = 2, Origen = "A", Destino = "B", Fecha = new DateOnly(2026, 1, 3), Estado = EstadoEnvio.Cancelado },
            new Envio { Id = 3, Origen = "C", Destino = "D", Fecha = new DateOnly(2026, 1, 4), Estado = EstadoEnvio.Entregado });
        db.Asignaciones.AddRange(
            new Asignacion { Id = 1, ViajeId = 1, EnvioId = 1, Ganancia = 18000 },
            new Asignacion { Id = 2, ViajeId = 1, EnvioId = 2, Ganancia = 9500 },
            new Asignacion { Id = 3, ViajeId = 2, EnvioId = 3, Ganancia = 2500 });
        await db.SaveChangesAsync();

        var resultado = await new HistorialService(db).ObtenerHistorialAsync(1, new HistorialFiltroDto());

        Assert.Equal(2, resultado.ViajesRealizados);
        Assert.Equal(2, resultado.EnviosTransportados);
        Assert.Equal(20500, resultado.GananciaEfectiva);
        Assert.Equal(2, resultado.Operaciones.Count);
        Assert.Equal(18000, resultado.Operaciones.Single(x => x.ViajeId == 1).Monto);
    }

    [Fact]
    public async Task Historial_aplica_filtros_y_aislamiento()
    {
        await using var db = CrearDb();
        db.Viajes.AddRange(
            new Viaje { Id = 1, UsuarioId = 1, Origen = "A", Destino = "B" },
            new Viaje { Id = 2, UsuarioId = 2, Origen = "C", Destino = "D" });
        db.Envios.AddRange(
            new Envio { Id = 1, Fecha = new DateOnly(2026, 2, 1), Estado = EstadoEnvio.Entregado },
            new Envio { Id = 2, Fecha = new DateOnly(2026, 3, 1), Estado = EstadoEnvio.Cancelado });
        db.Asignaciones.AddRange(
            new Asignacion { ViajeId = 1, EnvioId = 1, Ganancia = 100 },
            new Asignacion { ViajeId = 2, EnvioId = 2, Ganancia = 200 });
        await db.SaveChangesAsync();

        var resultado = await new HistorialService(db).ObtenerHistorialAsync(1, new HistorialFiltroDto
        {
            Desde = new DateOnly(2026, 2, 1),
            Hasta = new DateOnly(2026, 2, 28),
            Estado = EstadoEnvio.Entregado
        });

        var operacion = Assert.Single(resultado.Operaciones);
        Assert.Equal(1, operacion.ViajeId);
    }

    [Fact]
    public async Task Historial_aplica_paginacion()
    {
        await using var db = CrearDb();
        for (var i = 1; i <= 3; i++)
        {
            db.Viajes.Add(new Viaje { Id = i, UsuarioId = 1, Origen = $"A{i}", Destino = $"B{i}" });
            db.Envios.Add(new Envio { Id = i, Fecha = new DateOnly(2026, 1, i), Estado = EstadoEnvio.Entregado });
            db.Asignaciones.Add(new Asignacion { Id = i, ViajeId = i, EnvioId = i, Ganancia = i });
        }
        await db.SaveChangesAsync();

        var resultado = await new HistorialService(db).ObtenerHistorialAsync(1, new HistorialFiltroDto
        {
            Pagina = 2,
            PageSize = 2
        });

        Assert.Equal(3, resultado.TotalRegistros);
        Assert.Equal(2, resultado.TotalPaginas);
        Assert.Single(resultado.Operaciones);
    }

    private static RuteaYaDbContext CrearDb()
    {
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RuteaYaDbContext(options);
    }
}
