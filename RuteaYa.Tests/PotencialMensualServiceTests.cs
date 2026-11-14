using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Optimizacion;
using RuteaYa.Logica.Optimizacion.Criterios;
using RuteaYa.Logica.Servicios;
using Xunit;

namespace RuteaYa.Tests;

public sealed class PotencialMensualServiceTests
{
    private const int Conductor = 1;
    private const int OtroUsuario = 2;

    // Hoy es lunes 9/11/2026. Los martes que quedan en noviembre son 10, 17 y 24.
    private static readonly DateOnly Hoy = new(2026, 11, 9);
    private static readonly DateOnly Martes10 = new(2026, 11, 10);
    private static readonly DateOnly Martes17 = new(2026, 11, 17);
    private static readonly DateOnly Martes24 = new(2026, 11, 24);

    [Fact]
    public async Task Mes_con_varias_fechas_suma_cada_salida()
    {
        await using var db = CrearDbContext();
        AgregarViaje(db);
        AgregarEnvio(db, 10, Martes10, pago: 3000m);
        AgregarEnvio(db, 11, Martes17, pago: 2000m);
        AgregarEnvio(db, 12, Martes17, pago: 1500m);
        await db.SaveChangesAsync();

        var potencial = await CrearServicio(db).CalcularAsync(Conductor, 2026, 11, CriterioOptimizacion.MaximizarGanancias, default);

        Assert.Equal(Hoy, potencial.Desde);
        Assert.Equal(new DateOnly(2026, 11, 30), potencial.Hasta);
        Assert.Equal(1, potencial.ViajesRecurrentes);
        Assert.Equal([Martes10, Martes17, Martes24], potencial.Detalle.Select(salida => salida.Fecha));
        Assert.Equal(3, potencial.ViajesAnalizados);
        Assert.Equal(3, potencial.PaquetesPosibles);
        Assert.Equal(6500m, potencial.GananciaPotencial);
        Assert.Equal(2166.67m, potencial.PromedioPorViaje);
        Assert.Equal(0m, potencial.Detalle[2].Ganancia);

        Assert.Equal(Martes17, potencial.MejorRecorrido!.Fecha);
        Assert.Equal([11, 12], potencial.MejorRecorrido.EnvioIds.Order());
        Assert.Equal(3500m, potencial.MejorRecorrido.Ganancia);
        Assert.True(potencial.EsSimulacion);
    }

    [Fact]
    public async Task Ganancia_por_semana_va_de_lunes_a_domingo_recortada_al_mes()
    {
        await using var db = CrearDbContext();
        AgregarViaje(db);
        AgregarEnvio(db, 10, Martes10, pago: 3000m);
        AgregarEnvio(db, 11, Martes24, pago: 2000m);
        await db.SaveChangesAsync();

        var potencial = await CrearServicio(db).CalcularAsync(Conductor, 2026, 11, CriterioOptimizacion.MaximizarGanancias, default);

        Assert.Equal(
            [
                (new DateOnly(2026, 11, 9), new DateOnly(2026, 11, 15), 3000m),
                (new DateOnly(2026, 11, 16), new DateOnly(2026, 11, 22), 0m),
                (new DateOnly(2026, 11, 23), new DateOnly(2026, 11, 29), 2000m),
                (new DateOnly(2026, 11, 30), new DateOnly(2026, 11, 30), 0m)
            ],
            potencial.GananciaPorSemana.Select(semana => (semana.Desde, semana.Hasta, semana.Ganancia)));
    }

    [Fact]
    public async Task Un_envio_disputado_entre_dos_viajes_se_cuenta_una_sola_vez()
    {
        await using var db = CrearDbContext();
        var temprano = AgregarViaje(db, horaSalida: new TimeOnly(8, 0));
        var tarde = AgregarViaje(db, horaSalida: new TimeOnly(18, 0));
        AgregarEnvio(db, 10, Martes10, pago: 4000m);
        await db.SaveChangesAsync();

        var potencial = await CrearServicio(db).CalcularAsync(Conductor, 2026, 11, CriterioOptimizacion.MaximizarGanancias, default);

        Assert.Equal(6, potencial.ViajesAnalizados);   // 2 viajes x 3 martes
        Assert.Equal(1, potencial.PaquetesPosibles);
        Assert.Equal(4000m, potencial.GananciaPotencial);
        var conEnvio = Assert.Single(potencial.Detalle, salida => salida.EnvioIds.Count > 0);
        Assert.Equal(temprano.Id, conEnvio.ViajeId);   // lo toma la primera salida del día
        Assert.All(potencial.Detalle.Where(salida => salida.ViajeId == tarde.Id), salida => Assert.Empty(salida.EnvioIds));
    }

    [Fact]
    public async Task Lo_que_no_entra_en_un_viaje_queda_para_el_siguiente_del_mismo_dia()
    {
        await using var db = CrearDbContext();
        var temprano = AgregarViaje(db, horaSalida: new TimeOnly(8, 0), maxPaquetes: 1);
        var tarde = AgregarViaje(db, horaSalida: new TimeOnly(18, 0), maxPaquetes: 1);
        AgregarEnvio(db, 10, Martes10, pago: 5000m);
        AgregarEnvio(db, 11, Martes10, pago: 3000m);
        await db.SaveChangesAsync();

        var potencial = await CrearServicio(db).CalcularAsync(Conductor, 2026, 11, CriterioOptimizacion.MaximizarGanancias, default);

        Assert.Equal([10], potencial.Detalle.Single(s => s.ViajeId == temprano.Id && s.Fecha == Martes10).EnvioIds);
        Assert.Equal([11], potencial.Detalle.Single(s => s.ViajeId == tarde.Id && s.Fecha == Martes10).EnvioIds);
        Assert.Equal(8000m, potencial.GananciaPotencial);
        Assert.Equal(2, potencial.PaquetesPosibles);
    }

    [Fact]
    public async Task Sin_envios_disponibles_el_potencial_es_cero()
    {
        await using var db = CrearDbContext();
        AgregarViaje(db);
        // Ninguno sirve: ya tiene conductor, cae un día que el viaje no sale y es del propio conductor.
        AgregarEnvio(db, 10, Martes10, estado: EstadoEnvio.ConductorAsignado);
        AgregarEnvio(db, 11, Martes10.AddDays(1));
        AgregarEnvio(db, 12, Martes17, usuarioId: Conductor);
        await db.SaveChangesAsync();

        var potencial = await CrearServicio(db).CalcularAsync(Conductor, 2026, 11, CriterioOptimizacion.MaximizarGanancias, default);

        Assert.Equal(3, potencial.ViajesAnalizados);
        Assert.Equal(0, potencial.PaquetesPosibles);
        Assert.Equal(0m, potencial.GananciaPotencial);
        Assert.Equal(0m, potencial.PromedioPorViaje);
        Assert.Null(potencial.MejorRecorrido);
        Assert.All(potencial.Detalle, salida => Assert.Empty(salida.EnvioIds));
    }

    [Fact]
    public async Task Sin_viajes_activos_no_hay_salidas()
    {
        await using var db = CrearDbContext();
        AgregarViaje(db, estado: EstadoViaje.Inactivo);
        AgregarViaje(db, usuarioId: OtroUsuario);
        AgregarEnvio(db, 10, Martes10);
        await db.SaveChangesAsync();

        var potencial = await CrearServicio(db).CalcularAsync(Conductor, 2026, 11, CriterioOptimizacion.MaximizarGanancias, default);

        Assert.Equal(0, potencial.ViajesRecurrentes);
        Assert.Equal(0, potencial.ViajesAnalizados);
        Assert.Empty(potencial.Detalle);
        Assert.Equal(0m, potencial.GananciaPotencial);
    }

    [Fact]
    public async Task El_criterio_cambia_los_envios_elegidos()
    {
        await using var db = CrearDbContext();
        AgregarViaje(db);
        AgregarCasoEnunciado(db, Martes10);
        await db.SaveChangesAsync();
        var servicio = CrearServicio(db);

        var porGanancia = await servicio.CalcularAsync(Conductor, 2026, 11, CriterioOptimizacion.MaximizarGanancias, default);
        var porDesvio = await servicio.CalcularAsync(Conductor, 2026, 11, CriterioOptimizacion.MinimizarDesvios, default);

        Assert.Equal(CriterioOptimizacion.MaximizarGanancias, porGanancia.Criterio);
        Assert.Equal([1, 2, 5], porGanancia.MejorRecorrido!.EnvioIds);   // A+B+E
        Assert.Equal(12000m, porGanancia.GananciaPotencial);

        Assert.Equal(CriterioOptimizacion.MinimizarDesvios, porDesvio.Criterio);
        Assert.Equal([5], porDesvio.MejorRecorrido!.EnvioIds);           // E
        Assert.Equal(2500m, porDesvio.GananciaPotencial);
        Assert.NotEqual(porGanancia.CriterioDescripcion, porDesvio.CriterioDescripcion);
    }

    [Fact]
    public async Task Respeta_lo_ya_asignado_y_no_persiste_nada()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db, maxPaquetes: 2);
        AgregarEnvio(db, 1, Martes10, estado: EstadoEnvio.ConductorAsignado);
        db.Asignaciones.Add(new Asignacion { Viaje = viaje, EnvioId = 1, DesvioKm = 1m, Ganancia = 1000m });
        AgregarEnvio(db, 10, Martes10, pago: 3000m);
        AgregarEnvio(db, 11, Martes10, pago: 2000m);
        await db.SaveChangesAsync();

        var potencial = await CrearServicio(db).CalcularAsync(Conductor, 2026, 11, CriterioOptimizacion.MaximizarGanancias, default);

        // Queda un solo lugar el martes 10: entra el que más paga.
        Assert.Equal([10], potencial.Detalle.Single(salida => salida.Fecha == Martes10).EnvioIds);
        Assert.Equal(3000m, potencial.GananciaPotencial);

        Assert.Equal(1, await db.Asignaciones.CountAsync());
        Assert.All(
            await db.Envios.Where(envio => envio.Id >= 10).ToListAsync(),
            envio => Assert.Equal(EstadoEnvio.BuscandoConductor, envio.Estado));
    }

    [Fact]
    public async Task La_ganancia_efectiva_son_los_envios_entregados_del_mes_y_no_se_suma_a_la_potencial()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db);
        AgregarEnvio(db, 1, new DateOnly(2026, 11, 3), estado: EstadoEnvio.Entregado);
        AgregarEnvio(db, 2, new DateOnly(2026, 10, 27), estado: EstadoEnvio.Entregado);  // otro mes
        AgregarEnvio(db, 3, Martes10, estado: EstadoEnvio.ConductorAsignado);            // todavía no entregado
        db.Asignaciones.AddRange(
            new Asignacion { Viaje = viaje, EnvioId = 1, Ganancia = 4000m },
            new Asignacion { Viaje = viaje, EnvioId = 2, Ganancia = 9000m },
            new Asignacion { Viaje = viaje, EnvioId = 3, Ganancia = 1500m });
        await db.SaveChangesAsync();

        var potencial = await CrearServicio(db).CalcularAsync(Conductor, 2026, 11, CriterioOptimizacion.MaximizarGanancias, default);

        Assert.Equal(4000m, potencial.GananciaEfectiva);
        Assert.Equal(0m, potencial.GananciaPotencial);
    }

    [Fact]
    public async Task Un_mes_futuro_se_analiza_completo()
    {
        await using var db = CrearDbContext();
        AgregarViaje(db);
        await db.SaveChangesAsync();

        var potencial = await CrearServicio(db).CalcularAsync(Conductor, 2026, 12, CriterioOptimizacion.MaximizarGanancias, default);

        Assert.Equal(new DateOnly(2026, 12, 1), potencial.Desde);
        Assert.Equal(new DateOnly(2026, 12, 31), potencial.Hasta);
        Assert.Equal(5, potencial.ViajesAnalizados);   // martes 1, 8, 15, 22 y 29 de diciembre
    }

    [Theory]
    [InlineData(2026, 10)]   // ya terminó
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    [InlineData(1999, 5)]
    public async Task Mes_invalido_o_pasado_lanza_ValidacionException(int anio, int mes)
    {
        await using var db = CrearDbContext();

        await Assert.ThrowsAsync<ValidacionException>(() =>
            CrearServicio(db).CalcularAsync(Conductor, anio, mes, CriterioOptimizacion.MaximizarGanancias, default));
    }

    [Fact]
    public async Task Criterio_inexistente_lanza_ValidacionException()
    {
        await using var db = CrearDbContext();

        await Assert.ThrowsAsync<ValidacionException>(() =>
            CrearServicio(db).CalcularAsync(Conductor, 2026, 11, (CriterioOptimizacion)99, default));
    }

    // ---------- Helpers ----------

    private static PotencialMensualService CrearServicio(RuteaYaDbContext db)
    {
        var reloj = new RelojFijo(Hoy);
        var catalogo = new CatalogoCriterios([CriterioMaximizarGanancias.Instancia, new CriterioMinimizarDesvios(), new CriterioEquilibrio()]);

        return new PotencialMensualService(
            db,
            new OportunidadService(db, new RutasFalsas(), reloj),
            new OptimizacionService(new OptimizadorEnvios(), catalogo),
            catalogo,
            reloj);
    }

    private static RuteaYaDbContext CrearDbContext() =>
        new(new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Viaje AgregarViaje(
        RuteaYaDbContext db,
        TimeOnly? horaSalida = null,
        int maxPaquetes = 3,
        EstadoViaje estado = EstadoViaje.Activo,
        int usuarioId = Conductor)
    {
        var salida = horaSalida ?? new TimeOnly(8, 0);
        var viaje = new Viaje
        {
            UsuarioId = usuarioId,
            Origen = "Moron",
            Destino = "CABA",
            DiasSemana = DiasSemana.Martes,
            HoraSalida = salida,
            HoraLlegada = salida.AddHours(1),
            MaxPaquetes = maxPaquetes,
            PesoMaximoKg = 10m,
            DesvioMaximoKm = 5m,
            Estado = estado
        };
        db.Viajes.Add(viaje);
        return viaje;
    }

    /// <summary>Envíos A..E de la sección 12 del enunciado (ids 1..5).</summary>
    private static void AgregarCasoEnunciado(RuteaYaDbContext db, DateOnly fecha)
    {
        AgregarEnvio(db, 1, fecha, pago: 4000m, pesoKg: 2m, destino: "Al lado"); // A: 1 km
        AgregarEnvio(db, 2, fecha, pago: 5500m, pesoKg: 6m, destino: "A");       // B: 2 km
        AgregarEnvio(db, 3, fecha, pago: 3500m, pesoKg: 4m, destino: "Al lado"); // C: 1 km
        AgregarEnvio(db, 4, fecha, pago: 7000m, pesoKg: 8m, destino: "Cerca");   // D: 3 km
        AgregarEnvio(db, 5, fecha, pago: 2500m, pesoKg: 1m, destino: "Medio");   // E: 0,5 km
    }

    private static void AgregarEnvio(
        RuteaYaDbContext db,
        int id,
        DateOnly fecha,
        decimal pago = 1000m,
        decimal pesoKg = 1m,
        string destino = "Al lado",
        EstadoEnvio estado = EstadoEnvio.BuscandoConductor,
        int usuarioId = OtroUsuario)
    {
        db.Envios.Add(new Envio
        {
            Id = id,
            UsuarioId = usuarioId,
            Origen = "Haedo",
            Destino = destino,
            Fecha = fecha,
            PesoKg = pesoKg,
            Tamano = TamanoPaquete.Pequeno,
            Descripcion = "Caja",
            MontoOfrecido = pago,
            Estado = estado
        });
    }
}
