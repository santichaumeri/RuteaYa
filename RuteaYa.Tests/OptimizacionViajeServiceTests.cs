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

public sealed class OptimizacionViajeServiceTests
{
    private const int Conductor = 1;
    private const int OtroUsuario = 2;

    // Lunes 9/11/2026. El viaje sale lunes y miércoles.
    private static readonly DateOnly Hoy = new(2026, 11, 9);
    private static readonly DateOnly Lunes = Hoy;
    private static readonly DateOnly Miercoles = Hoy.AddDays(2);
    private static readonly DateOnly LunesQueViene = Hoy.AddDays(7);

    [Fact]
    public async Task Calcula_la_combinacion_del_enunciado_para_el_viaje()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db);
        AgregarCasoEnunciado(db, Lunes);
        await db.SaveChangesAsync();

        var respuesta = await CrearServicio(db).CalcularAsync(viaje.Id, Conductor, null, CriterioOptimizacion.MaximizarGanancias, default);

        Assert.Equal(Lunes, respuesta.Fecha);
        Assert.Equal(5, respuesta.Candidatos.Count);
        var mejor = respuesta.Resultado!.MejorCombinacion!;
        Assert.Equal([1, 2, 5], mejor.EnvioIds);   // A+B+E
        Assert.Equal(12000m, mejor.GananciaTotal);
        Assert.Equal(9m, mejor.PesoTotalKg);
        Assert.Equal(3.5m, mejor.DesvioTotalKm);
        Assert.NotEmpty(respuesta.Resultado.Alternativas);
    }

    [Fact]
    public async Task El_criterio_cambia_la_combinacion_recomendada()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db);
        AgregarCasoEnunciado(db, Lunes);
        await db.SaveChangesAsync();

        var respuesta = await CrearServicio(db).CalcularAsync(viaje.Id, Conductor, null, CriterioOptimizacion.MinimizarDesvios, default);

        Assert.Equal(CriterioOptimizacion.MinimizarDesvios, respuesta.Resultado!.Criterio);
        Assert.Equal([5], respuesta.Resultado.MejorCombinacion!.EnvioIds);
    }

    [Fact]
    public async Task Sin_fecha_usa_la_mas_proxima_y_solo_combina_envios_de_ese_dia()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db);
        AgregarEnvio(db, 10, LunesQueViene, pago: 9000m);
        AgregarEnvio(db, 11, Miercoles, pago: 1000m);
        AgregarEnvio(db, 12, Miercoles, pago: 2000m);
        await db.SaveChangesAsync();

        var respuesta = await CrearServicio(db).CalcularAsync(viaje.Id, Conductor, null, CriterioOptimizacion.MaximizarGanancias, default);

        Assert.Equal([Miercoles, LunesQueViene], respuesta.FechasConOportunidades);
        Assert.Equal(Miercoles, respuesta.Fecha);
        Assert.Equal([11, 12], respuesta.Candidatos.Select(x => x.EnvioId).Order());
        Assert.Equal([11, 12], respuesta.Resultado!.MejorCombinacion!.EnvioIds);
    }

    [Fact]
    public async Task Con_fecha_optimiza_ese_dia()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db);
        AgregarEnvio(db, 10, LunesQueViene, pago: 9000m);
        AgregarEnvio(db, 11, Miercoles, pago: 1000m);
        await db.SaveChangesAsync();

        var respuesta = await CrearServicio(db).CalcularAsync(viaje.Id, Conductor, LunesQueViene, CriterioOptimizacion.MaximizarGanancias, default);

        Assert.Equal(LunesQueViene, respuesta.Fecha);
        Assert.Equal([10], respuesta.Resultado!.MejorCombinacion!.EnvioIds);
    }

    [Fact]
    public async Task Los_limites_descuentan_lo_ya_asignado_ese_dia()
    {
        await using var db = CrearDbContext();
        // 3 paquetes, 10 kg y 5 km; el lunes ya hay un envío de 7 kg con 1 km de desvío.
        var viaje = AgregarViaje(db);
        AgregarEnvio(db, 1, Lunes, pesoKg: 7m, destino: "Al lado", estado: EstadoEnvio.ConductorAsignado);
        db.Asignaciones.Add(new Asignacion { Viaje = viaje, EnvioId = 1, DesvioKm = 1m, Ganancia = 1000m });
        AgregarEnvio(db, 10, Lunes, pesoKg: 2m, pago: 3000m, destino: "Al lado");
        AgregarEnvio(db, 11, Lunes, pesoKg: 2m, pago: 2500m, destino: "Al lado");
        await db.SaveChangesAsync();

        var respuesta = await CrearServicio(db).CalcularAsync(viaje.Id, Conductor, Lunes, CriterioOptimizacion.MaximizarGanancias, default);

        Assert.Equal(1, respuesta.PaquetesAsignados);
        Assert.Equal(7m, respuesta.PesoAsignadoKg);
        Assert.Equal(1m, respuesta.DesvioAsignadoKm);
        // Quedan 3 kg: los dos juntos (4 kg) no entran, gana el que más paga.
        Assert.Equal([10], respuesta.Resultado!.MejorCombinacion!.EnvioIds);
        Assert.Contains(respuesta.Resultado.Descartadas, d => d.EnvioIds.SequenceEqual([10, 11]));
    }

    [Fact]
    public async Task Sin_oportunidades_no_hay_fecha_ni_resultado()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db);
        await db.SaveChangesAsync();

        var respuesta = await CrearServicio(db).CalcularAsync(viaje.Id, Conductor, null, CriterioOptimizacion.MaximizarGanancias, default);

        Assert.Empty(respuesta.FechasConOportunidades);
        Assert.Null(respuesta.Fecha);
        Assert.Null(respuesta.Resultado);
        Assert.Equal(3, respuesta.MaxPaquetes);
    }

    [Fact]
    public async Task Fecha_sin_candidatos_devuelve_resultado_nulo()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db);
        AgregarEnvio(db, 10, Lunes);
        await db.SaveChangesAsync();

        var respuesta = await CrearServicio(db).CalcularAsync(viaje.Id, Conductor, Miercoles, CriterioOptimizacion.MaximizarGanancias, default);

        Assert.Equal(Miercoles, respuesta.Fecha);
        Assert.Empty(respuesta.Candidatos);
        Assert.Null(respuesta.Resultado);
    }

    [Fact]
    public async Task Viaje_inexistente_lanza_NotFound_y_ajeno_AccesoDenegado()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db);
        await db.SaveChangesAsync();
        var servicio = CrearServicio(db);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            servicio.CalcularAsync(999, Conductor, null, CriterioOptimizacion.MaximizarGanancias, default));
        await Assert.ThrowsAsync<AccesoDenegadoException>(() =>
            servicio.CalcularAsync(viaje.Id, OtroUsuario, null, CriterioOptimizacion.MaximizarGanancias, default));
    }

    // ---------- Helpers ----------

    private static OptimizacionViajeService CrearServicio(RuteaYaDbContext db) =>
        new(
            new OportunidadService(db, new RutasFalsas(), new RelojFijo(Hoy)),
            new OptimizacionService(
                new OptimizadorEnvios(),
                new CatalogoCriterios([CriterioMaximizarGanancias.Instancia, new CriterioMinimizarDesvios(), new CriterioEquilibrio()])));

    private static RuteaYaDbContext CrearDbContext() =>
        new(new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Viaje AgregarViaje(RuteaYaDbContext db)
    {
        var viaje = new Viaje
        {
            UsuarioId = Conductor,
            Origen = "Moron",
            Destino = "CABA",
            DiasSemana = DiasSemana.Lunes | DiasSemana.Miercoles,
            HoraSalida = new TimeOnly(8, 0),
            HoraLlegada = new TimeOnly(9, 0),
            MaxPaquetes = 3,
            PesoMaximoKg = 10m,
            DesvioMaximoKm = 5m,
            Estado = EstadoViaje.Activo
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
        EstadoEnvio estado = EstadoEnvio.BuscandoConductor)
    {
        db.Envios.Add(new Envio
        {
            Id = id,
            UsuarioId = OtroUsuario,
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
