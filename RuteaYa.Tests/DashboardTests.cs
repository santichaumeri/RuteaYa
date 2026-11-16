using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos.Potencial;
using RuteaYa.Logica.Optimizacion.Criterios;
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

        var resumen = await CrearServicio(dbContext).ObtenerResumenAsync(usuario.Id);

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

        var resumen = await CrearServicio(dbContext).ObtenerResumenAsync(usuario.Id);

        Assert.NotNull(resumen);
        Assert.Equal(0, resumen.ProximosViajes);
        Assert.Empty(resumen.Viajes);
        Assert.Equal(0, resumen.EnviosPendientes);
        Assert.Empty(resumen.Envios);
    }

    [Fact]
    public async Task Potencial_del_mes_sale_del_calculo_mensual_del_mes_en_curso()
    {
        await using var dbContext = CrearDbContext();
        dbContext.Usuarios.Add(new Usuario { Id = 1, Nombre = "Juan", Apellido = "Pérez", Email = "juan@example.com" });
        await dbContext.SaveChangesAsync();
        var potencial = new PotencialFijo(8500m);

        var resumen = await CrearServicio(dbContext, potencial).ObtenerResumenAsync(1);

        Assert.NotNull(resumen);
        Assert.Equal(8500m, resumen.PotencialMes);
        Assert.Equal((1, 2026, 11, CriterioOptimizacion.MaximizarGanancias), potencial.UltimaConsulta);
    }

    [Fact]
    public async Task Oportunidades_cuenta_una_vez_cada_envio_compatible_con_algun_viaje_activo()
    {
        await using var dbContext = CrearDbContext();
        dbContext.Usuarios.AddRange(
            new Usuario { Id = 1, Nombre = "Juan", Apellido = "Pérez", Email = "juan@example.com" },
            new Usuario { Id = 2, Nombre = "Ana", Apellido = "López", Email = "ana@example.com" });
        dbContext.Viajes.AddRange(
            ViajeDe(1, DiasSemana.Lunes, EstadoViaje.Activo),
            ViajeDe(1, DiasSemana.Lunes | DiasSemana.Martes, EstadoViaje.Activo),
            ViajeDe(1, DiasSemana.Miercoles, EstadoViaje.Inactivo));
        dbContext.Envios.AddRange(
            // Le sirve a los dos viajes del lunes: se cuenta una sola vez.
            EnvioDe(10, usuarioId: 2, destino: "Cerca", fecha: Lunes),
            // Solo el segundo viaje sale los martes.
            EnvioDe(11, usuarioId: 2, destino: "A", fecha: Lunes.AddDays(1)),
            // 6 km de desvío: supera el máximo de 5 km de los viajes.
            EnvioDe(12, usuarioId: 2, destino: "MuyLejos", fecha: Lunes),
            // Solo lo llevaría el viaje inactivo de los miércoles.
            EnvioDe(13, usuarioId: 2, destino: "Cerca", fecha: Lunes.AddDays(2)),
            // Es del propio usuario: no es una oportunidad para él.
            EnvioDe(14, usuarioId: 1, destino: "Cerca", fecha: Lunes));
        await dbContext.SaveChangesAsync();

        var resumen = await CrearServicio(dbContext).ObtenerResumenAsync(1);

        Assert.NotNull(resumen);
        Assert.Equal(2, resumen.Oportunidades);
        Assert.Equal(2, resumen.ProximosViajes);
    }

    [Fact]
    public async Task Oportunidades_es_cero_sin_viajes_activos()
    {
        await using var dbContext = CrearDbContext();
        dbContext.Usuarios.AddRange(
            new Usuario { Id = 1, Nombre = "Juan", Apellido = "Pérez", Email = "juan@example.com" },
            new Usuario { Id = 2, Nombre = "Ana", Apellido = "López", Email = "ana@example.com" });
        dbContext.Viajes.Add(ViajeDe(1, DiasSemana.Lunes, EstadoViaje.Inactivo));
        dbContext.Envios.Add(EnvioDe(10, usuarioId: 2, destino: "Cerca", fecha: Lunes));
        await dbContext.SaveChangesAsync();

        var resumen = await CrearServicio(dbContext).ObtenerResumenAsync(1);

        Assert.NotNull(resumen);
        Assert.Equal(0, resumen.Oportunidades);
    }

    /// <summary>Lunes 9 de noviembre de 2026, el "hoy" de estos tests.</summary>
    private static readonly DateOnly Hoy = new(2026, 11, 9);

    /// <summary>El lunes siguiente, para que los envíos sean futuros.</summary>
    private static readonly DateOnly Lunes = Hoy.AddDays(7);

    private static Viaje ViajeDe(int usuarioId, DiasSemana dias, EstadoViaje estado) => new()
    {
        UsuarioId = usuarioId,
        Origen = "Moron",
        Destino = "CABA",
        DiasSemana = dias,
        HoraSalida = new TimeOnly(8, 0),
        HoraLlegada = new TimeOnly(9, 0),
        MaxPaquetes = 3,
        PesoMaximoKg = 10,
        DesvioMaximoKm = 5,
        Estado = estado
    };

    private static Envio EnvioDe(int id, int usuarioId, string destino, DateOnly fecha) => new()
    {
        Id = id,
        UsuarioId = usuarioId,
        Origen = "Haedo",
        Destino = destino,
        Fecha = fecha,
        PesoKg = 1,
        Tamano = TamanoPaquete.Pequeno,
        Descripcion = "Caja",
        MontoOfrecido = 1000,
        Estado = EstadoEnvio.BuscandoConductor
    };

    /// <summary>
    /// Las oportunidades usan el servicio real con rutas falsas, así el número del dashboard
    /// es el mismo que mostraría la pantalla de Oportunidades.
    /// </summary>
    private static DashboardService CrearServicio(RuteaYaDbContext dbContext, PotencialFijo? potencial = null)
    {
        var reloj = new RelojFijo(Hoy);
        return new(dbContext, potencial ?? new PotencialFijo(0m), new OportunidadService(dbContext, new RutasFalsas(), reloj), reloj);
    }

    /// <summary>Doble del potencial mensual: el cálculo real ya se prueba en PotencialMensualServiceTests.</summary>
    private sealed class PotencialFijo(decimal ganancia) : IPotencialMensualService
    {
        public (int UsuarioId, int Anio, int Mes, CriterioOptimizacion Criterio)? UltimaConsulta { get; private set; }

        public Task<PotencialMensualResponse> CalcularAsync(
            int usuarioId, int anio, int mes, CriterioOptimizacion criterio, CancellationToken cancellationToken)
        {
            UltimaConsulta = (usuarioId, anio, mes, criterio);
            return Task.FromResult(new PotencialMensualResponse { Anio = anio, Mes = mes, GananciaPotencial = ganancia });
        }
    }

    private static RuteaYaDbContext CrearDbContext()
    {
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new RuteaYaDbContext(options);
    }
}
