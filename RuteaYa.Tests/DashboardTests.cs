using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Servicios;
using Xunit;

namespace RuteaYa.Tests;

public sealed class DashboardTests
{
    [Fact]
    public async Task Resumen_solo_incluye_datos_del_usuario_solicitado()
    {
        await using var dbContext = CrearDbContext();
        var usuario = new Usuario { Id = 1, Nombre = "Juan", Apellido = "Pérez", Email = "juan@example.com" };
        var otroUsuario = new Usuario { Id = 2, Nombre = "Ana", Apellido = "López", Email = "ana@example.com" };
        dbContext.Usuarios.AddRange(usuario, otroUsuario);
        dbContext.Viajes.AddRange(
            new Viaje
            {
                Id = 1,
                UsuarioId = usuario.Id,
                Origen = "Belgrano",
                Destino = "Lomas de Zamora",
                DiasSemana = DiasSemana.Lunes,
                HoraSalida = new TimeOnly(8, 0),
                HoraLlegada = new TimeOnly(8, 55),
                Estado = EstadoViaje.Activo
            },
            new Viaje
            {
                Id = 2,
                UsuarioId = otroUsuario.Id,
                Origen = "Palermo",
                Destino = "San Isidro",
                Estado = EstadoViaje.Activo
            });
        dbContext.Envios.Add(
            new Envio
            {
                Id = 1,
                UsuarioId = usuario.Id,
                Origen = "Plaza Italia",
                Destino = "Obelisco",
                Fecha = new DateOnly(2026, 11, 1),
                Descripcion = "Zapatillas",
                MontoOfrecido = 4000,
                Estado = EstadoEnvio.BuscandoConductor
            });
        await dbContext.SaveChangesAsync();

        var resumen = await new DashboardService(dbContext).ObtenerResumenAsync(usuario.Id);

        Assert.NotNull(resumen);
        Assert.Equal("Juan", resumen.NombreUsuario);
        Assert.Equal(1, resumen.ProximosViajes);
        Assert.Single(resumen.Viajes);
        Assert.Equal(1, resumen.EnviosPendientes);
        Assert.Single(resumen.Envios);
    }

    [Fact]
    public async Task Resumen_excluye_viajes_inactivos_y_envios_finalizados()
    {
        await using var dbContext = CrearDbContext();
        var usuario = new Usuario { Id = 1, Nombre = "Juan", Apellido = "Pérez", Email = "juan@example.com" };
        dbContext.Usuarios.Add(usuario);
        dbContext.Viajes.Add(new Viaje
        {
            UsuarioId = usuario.Id,
            Estado = EstadoViaje.Inactivo
        });
        dbContext.Envios.AddRange(
            new Envio { UsuarioId = usuario.Id, Estado = EstadoEnvio.Entregado },
            new Envio { UsuarioId = usuario.Id, Estado = EstadoEnvio.Cancelado });
        await dbContext.SaveChangesAsync();

        var resumen = await new DashboardService(dbContext).ObtenerResumenAsync(usuario.Id);

        Assert.NotNull(resumen);
        Assert.Equal(0, resumen.ProximosViajes);
        Assert.Empty(resumen.Viajes);
        Assert.Equal(0, resumen.EnviosPendientes);
        Assert.Empty(resumen.Envios);
    }

    private static RuteaYaDbContext CrearDbContext()
    {
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new RuteaYaDbContext(options);
    }
}
