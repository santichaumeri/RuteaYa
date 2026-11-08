using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos.Oportunidades;
using RuteaYa.Logica.Dtos.Rutas;
using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Oportunidades;
using RuteaYa.Logica.Servicios;
using RuteaYa.Logica.Servicios.Rutas;
using Xunit;

namespace RuteaYa.Tests;

public sealed class OportunidadServiceTests
{
    private const int Conductor = 1;
    private const int OtroUsuario = 2;

    // Lunes 9/11/2026. El viaje sale lunes y miércoles.
    private static readonly DateOnly Hoy = new(2026, 11, 9);
    private static readonly DateOnly Lunes = Hoy;
    private static readonly DateOnly Martes = Hoy.AddDays(1);
    private static readonly DateOnly Miercoles = Hoy.AddDays(2);
    private static readonly DateOnly LunesPasado = Hoy.AddDays(-7);

    // ---------- HU-11: búsqueda ----------

    [Fact]
    public async Task Buscar_solo_devuelve_envios_buscando_conductor()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db);
        AgregarEnvio(db, 10, destino: "A");
        AgregarEnvio(db, 11, destino: "A", estado: EstadoEnvio.Cancelado);
        AgregarEnvio(db, 12, destino: "A", estado: EstadoEnvio.ConductorAsignado);
        AgregarEnvio(db, 13, destino: "A", estado: EstadoEnvio.Entregado);
        await db.SaveChangesAsync();

        var resultado = await CrearServicio(db).BuscarAsync(viaje.Id, Conductor, OrdenOportunidades.Ganancia, default);

        Assert.Equal([10], resultado.Select(x => x.EnvioId));
    }

    [Fact]
    public async Task Buscar_no_muestra_los_envios_del_propio_conductor()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db);
        AgregarEnvio(db, 10, destino: "A");
        AgregarEnvio(db, 11, destino: "A", usuarioId: Conductor);
        await db.SaveChangesAsync();

        var resultado = await CrearServicio(db).BuscarAsync(viaje.Id, Conductor, OrdenOportunidades.Ganancia, default);

        Assert.Equal([10], resultado.Select(x => x.EnvioId));
    }

    [Fact]
    public async Task Buscar_respeta_los_dias_del_viaje_y_descarta_fechas_pasadas()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db);
        AgregarEnvio(db, 10, destino: "A", fecha: Lunes);
        AgregarEnvio(db, 11, destino: "A", fecha: Martes);      // el viaje no sale los martes
        AgregarEnvio(db, 12, destino: "A", fecha: Miercoles);
        AgregarEnvio(db, 13, destino: "A", fecha: LunesPasado); // ya pasó
        await db.SaveChangesAsync();

        var resultado = await CrearServicio(db).BuscarAsync(viaje.Id, Conductor, OrdenOportunidades.Ganancia, default);

        Assert.Equal([10, 12], resultado.Select(x => x.EnvioId).Order());
    }

    [Fact]
    public async Task Buscar_descuenta_peso_y_paquetes_ya_asignados_ese_dia()
    {
        await using var db = CrearDbContext();
        // Máximo 2 paquetes y 10 kg por viaje.
        var viaje = AgregarViaje(db, maxPaquetes: 2, pesoMaximoKg: 10m);
        AgregarAsignado(db, viaje, 1, Lunes, pesoKg: 7m, desvioKm: 1m);
        AgregarEnvio(db, 10, destino: "A", fecha: Lunes, pesoKg: 4m);     // 7 + 4 > 10
        AgregarEnvio(db, 11, destino: "A", fecha: Lunes, pesoKg: 3m);     // 7 + 3 = 10, entra
        AgregarEnvio(db, 12, destino: "A", fecha: Miercoles, pesoKg: 4m); // otro día: viaje vacío
        await db.SaveChangesAsync();

        var resultado = await CrearServicio(db).BuscarAsync(viaje.Id, Conductor, OrdenOportunidades.Ganancia, default);

        Assert.Equal([11, 12], resultado.Select(x => x.EnvioId).Order());
    }

    [Fact]
    public async Task Buscar_descarta_cuando_el_viaje_ya_completo_los_paquetes_del_dia()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db, maxPaquetes: 1);
        AgregarAsignado(db, viaje, 1, Lunes, pesoKg: 1m, desvioKm: 0m);
        AgregarEnvio(db, 10, destino: "A", fecha: Lunes);
        AgregarEnvio(db, 11, destino: "A", fecha: Miercoles);
        await db.SaveChangesAsync();

        var resultado = await CrearServicio(db).BuscarAsync(viaje.Id, Conductor, OrdenOportunidades.Ganancia, default);

        Assert.Equal([11], resultado.Select(x => x.EnvioId));
    }

    [Fact]
    public async Task Buscar_ignora_asignaciones_de_envios_cancelados()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db, maxPaquetes: 1);
        AgregarAsignado(db, viaje, 1, Lunes, pesoKg: 1m, desvioKm: 0m, estado: EstadoEnvio.Cancelado);
        AgregarEnvio(db, 10, destino: "A", fecha: Lunes);
        await db.SaveChangesAsync();

        var resultado = await CrearServicio(db).BuscarAsync(viaje.Id, Conductor, OrdenOportunidades.Ganancia, default);

        Assert.Equal([10], resultado.Select(x => x.EnvioId));
    }

    [Fact]
    public async Task Buscar_respeta_el_desvio_maximo_incluido_el_ya_consumido()
    {
        await using var db = CrearDbContext();
        // Desvío máximo 5 km; ya hay 2 km comprometidos el lunes.
        var viaje = AgregarViaje(db, desvioMaximoKm: 5m);
        AgregarAsignado(db, viaje, 1, Lunes, pesoKg: 1m, desvioKm: 2m);
        AgregarEnvio(db, 10, destino: "Cerca", fecha: Lunes);    // 3 km: 2 + 3 = 5, entra
        AgregarEnvio(db, 11, destino: "Lejos", fecha: Lunes);    // 4 km: 2 + 4 > 5
        AgregarEnvio(db, 12, destino: "Lejos", fecha: Miercoles); // 4 km con el viaje vacío, entra
        AgregarEnvio(db, 13, destino: "MuyLejos", fecha: Miercoles); // 6 km > 5
        await db.SaveChangesAsync();

        var resultado = await CrearServicio(db).BuscarAsync(viaje.Id, Conductor, OrdenOportunidades.Ganancia, default);

        Assert.Equal([10, 12], resultado.Select(x => x.EnvioId).Order());
    }

    [Fact]
    public async Task Buscar_ordena_por_ganancia_desvio_o_compatibilidad()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db, desvioMaximoKm: 5m);
        AgregarEnvio(db, 10, destino: "Borde", pago: 9000m);   // 4,5 km: Mucho desvío, $2000/km
        AgregarEnvio(db, 11, destino: "Cerca", pago: 3000m);   // 3 km: Compatible, $1000/km
        AgregarEnvio(db, 12, destino: "Al lado", pago: 1000m); // 1 km: Muy compatible, $1000/km
        AgregarEnvio(db, 13, destino: "Cerca", pago: 6000m);   // 3 km: Compatible, $2000/km
        await db.SaveChangesAsync();
        var servicio = CrearServicio(db);

        var porGanancia = await servicio.BuscarAsync(viaje.Id, Conductor, OrdenOportunidades.Ganancia, default);
        var porDesvio = await servicio.BuscarAsync(viaje.Id, Conductor, OrdenOportunidades.Desvio, default);
        var porCompatibilidad = await servicio.BuscarAsync(viaje.Id, Conductor, OrdenOportunidades.Compatibilidad, default);

        Assert.Equal([10, 13, 11, 12], porGanancia.Select(x => x.EnvioId));
        Assert.Equal([12, 13, 11, 10], porDesvio.Select(x => x.EnvioId));
        Assert.Equal([12, 13, 11, 10], porCompatibilidad.Select(x => x.EnvioId));
        Assert.Equal("Mucho desvío", porGanancia[0].Compatibilidad);
        Assert.Equal("Muy compatible", porDesvio[0].Compatibilidad);
    }

    [Fact]
    public async Task Buscar_en_viaje_ajeno_lanza_AccesoDenegado_y_en_inexistente_NotFound()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db);
        await db.SaveChangesAsync();
        var servicio = CrearServicio(db);

        await Assert.ThrowsAsync<AccesoDenegadoException>(() =>
            servicio.BuscarAsync(viaje.Id, OtroUsuario, OrdenOportunidades.Ganancia, default));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            servicio.BuscarAsync(999, Conductor, OrdenOportunidades.Ganancia, default));
    }

    [Fact]
    public async Task Buscar_en_viaje_inactivo_lanza_ConflictException()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db);
        viaje.Estado = EstadoViaje.Inactivo;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            CrearServicio(db).BuscarAsync(viaje.Id, Conductor, OrdenOportunidades.Ganancia, default));
    }

    // ---------- HU-12: detalle ----------

    [Fact]
    public async Task Detalle_calcula_las_metricas_de_la_oportunidad()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db, maxPaquetes: 3, pesoMaximoKg: 10m, desvioMaximoKm: 5m);
        AgregarAsignado(db, viaje, 1, Lunes, pesoKg: 4m, desvioKm: 1m);
        AgregarEnvio(db, 10, destino: "Cerca", pago: 6000m, pesoKg: 2m);
        await db.SaveChangesAsync();

        var detalle = await CrearServicio(db).ObtenerDetalleAsync(viaje.Id, 10, Conductor, default);

        Assert.Equal(10m, detalle.DistanciaHabitualKm);
        Assert.Equal(13m, detalle.DistanciaConEnvioKm);
        Assert.Equal(3m, detalle.DesvioKm);
        Assert.Equal(TimeSpan.FromMinutes(15), detalle.TiempoHabitual);
        Assert.Equal(TimeSpan.FromMinutes(4.5), detalle.TiempoAdicional);
        Assert.Equal(6000m, detalle.Pago);
        Assert.Equal(2m, detalle.PesoKg);
        Assert.Equal(2, detalle.PaquetesDisponibles);
        Assert.Equal(6m, detalle.PesoDisponibleKg);
        Assert.Equal(4m, detalle.DesvioDisponibleKm);
        Assert.Equal(2000m, detalle.GananciaPorKmAdicional);
        Assert.Equal("Compatible", detalle.Compatibilidad);
        Assert.True(detalle.PuedeAceptarse);
        Assert.Empty(detalle.MotivosBloqueo);
    }

    [Fact]
    public async Task Detalle_coincide_con_lo_que_muestra_la_busqueda()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db);
        AgregarEnvio(db, 10, destino: "Cerca", pago: 4500m);
        await db.SaveChangesAsync();
        var servicio = CrearServicio(db);

        var enLista = Assert.Single(await servicio.BuscarAsync(viaje.Id, Conductor, OrdenOportunidades.Ganancia, default));
        var detalle = await servicio.ObtenerDetalleAsync(viaje.Id, 10, Conductor, default);

        Assert.Equal(enLista.DesvioKm, detalle.DesvioKm);
        Assert.Equal(enLista.TiempoAdicional, detalle.TiempoAdicional);
        Assert.Equal(enLista.GananciaPorKmAdicional, detalle.GananciaPorKmAdicional);
        Assert.Equal(enLista.Compatibilidad, detalle.Compatibilidad);
    }

    [Fact]
    public async Task Detalle_sin_desvio_no_divide_por_cero()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db);
        AgregarEnvio(db, 10, destino: "En el camino", pago: 2000m);
        await db.SaveChangesAsync();

        var detalle = await CrearServicio(db).ObtenerDetalleAsync(viaje.Id, 10, Conductor, default);

        Assert.Equal(0m, detalle.DesvioKm);
        Assert.Null(detalle.GananciaPorKmAdicional);
        Assert.Equal("Muy compatible", detalle.Compatibilidad);
        Assert.True(detalle.PuedeAceptarse);
    }

    [Fact]
    public async Task Detalle_de_envio_que_ya_fue_tomado_bloquea_la_aceptacion_con_motivo()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db);
        AgregarEnvio(db, 10, destino: "Cerca", estado: EstadoEnvio.ConductorAsignado);
        await db.SaveChangesAsync();

        var detalle = await CrearServicio(db).ObtenerDetalleAsync(viaje.Id, 10, Conductor, default);

        Assert.False(detalle.PuedeAceptarse);
        Assert.Contains(detalle.MotivosBloqueo, m => m.Contains("ya no está disponible"));
    }

    [Fact]
    public async Task Detalle_informa_todos_los_motivos_que_dejaron_de_cumplirse()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db, pesoMaximoKg: 10m, desvioMaximoKm: 5m);
        AgregarEnvio(db, 10, destino: "MuyLejos", fecha: Martes, pesoKg: 12m);
        await db.SaveChangesAsync();

        var detalle = await CrearServicio(db).ObtenerDetalleAsync(viaje.Id, 10, Conductor, default);

        Assert.False(detalle.PuedeAceptarse);
        Assert.Contains(detalle.MotivosBloqueo, m => m.Contains("no sale el martes"));
        Assert.Contains(detalle.MotivosBloqueo, m => m.Contains("peso disponible"));
        Assert.Contains(detalle.MotivosBloqueo, m => m.Contains("desvío disponible"));
    }

    [Fact]
    public async Task Detalle_de_envio_inexistente_lanza_NotFound_y_de_viaje_ajeno_AccesoDenegado()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db);
        AgregarEnvio(db, 10, destino: "Cerca");
        await db.SaveChangesAsync();
        var servicio = CrearServicio(db);

        await Assert.ThrowsAsync<NotFoundException>(() => servicio.ObtenerDetalleAsync(viaje.Id, 999, Conductor, default));
        await Assert.ThrowsAsync<AccesoDenegadoException>(() => servicio.ObtenerDetalleAsync(viaje.Id, 10, OtroUsuario, default));
    }

    [Fact]
    public async Task Verificar_lanza_ConflictException_si_el_envio_dejo_de_ser_compatible()
    {
        await using var db = CrearDbContext();
        var viaje = AgregarViaje(db, maxPaquetes: 1);
        AgregarEnvio(db, 10, destino: "Cerca", fecha: Lunes);
        AgregarEnvio(db, 11, destino: "Cerca", fecha: Lunes);
        await db.SaveChangesAsync();
        var servicio = CrearServicio(db);

        await servicio.VerificarCompatibilidadAsync(viaje.Id, 10, Conductor, default); // todavía entra

        // Mientras el conductor miraba el detalle, otro envío ocupó el único lugar del lunes.
        var ocupante = await db.Envios.SingleAsync(x => x.Id == 11);
        ocupante.Estado = EstadoEnvio.ConductorAsignado;
        db.Asignaciones.Add(new Asignacion { ViajeId = viaje.Id, EnvioId = 11, DesvioKm = 3m, Ganancia = 1000m });
        await db.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            servicio.VerificarCompatibilidadAsync(viaje.Id, 10, Conductor, default));
        Assert.Contains("cantidad máxima de paquetes", error.Message);
    }

    // ---------- Reglas ----------

    [Theory]
    [InlineData(0, "Muy compatible")]
    [InlineData(2, "Muy compatible")]
    [InlineData(2.01, "Compatible")]
    [InlineData(4, "Compatible")]
    [InlineData(4.01, "Mucho desvío")]
    [InlineData(5, "Mucho desvío")]
    public void Clasificar_usa_la_proporcion_del_desvio_maximo(decimal desvio, string esperado)
    {
        var nivel = ReglasCompatibilidad.Clasificar(desvio, 5m);

        Assert.Equal(esperado, ReglasCompatibilidad.Etiqueta(nivel));
    }

    [Fact]
    public void GananciaPorKm_con_desvio_cero_es_null()
    {
        Assert.Null(ReglasCompatibilidad.GananciaPorKmAdicional(5000m, 0m));
        Assert.Equal(1666.67m, ReglasCompatibilidad.GananciaPorKmAdicional(5000m, 3m));
    }

    // ---------- Helpers ----------

    private static OportunidadService CrearServicio(RuteaYaDbContext db) =>
        new(db, new RutasFalsas(), new RelojFijo(Hoy));

    private static RuteaYaDbContext CrearDbContext() =>
        new(new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Viaje AgregarViaje(
        RuteaYaDbContext db,
        int maxPaquetes = 3,
        decimal pesoMaximoKg = 20m,
        decimal desvioMaximoKm = 5m)
    {
        var viaje = new Viaje
        {
            UsuarioId = Conductor,
            Origen = "Moron",
            Destino = "CABA",
            DiasSemana = DiasSemana.Lunes | DiasSemana.Miercoles,
            HoraSalida = new TimeOnly(8, 0),
            HoraLlegada = new TimeOnly(9, 0),
            MaxPaquetes = maxPaquetes,
            PesoMaximoKg = pesoMaximoKg,
            DesvioMaximoKm = desvioMaximoKm,
            Estado = EstadoViaje.Activo
        };
        db.Viajes.Add(viaje);
        return viaje;
    }

    private static Envio AgregarEnvio(
        RuteaYaDbContext db,
        int id,
        string destino,
        DateOnly? fecha = null,
        decimal pesoKg = 1m,
        decimal pago = 1000m,
        EstadoEnvio estado = EstadoEnvio.BuscandoConductor,
        int usuarioId = OtroUsuario)
    {
        var envio = new Envio
        {
            Id = id,
            UsuarioId = usuarioId,
            Origen = "Haedo",
            Destino = destino,
            Fecha = fecha ?? Lunes,
            PesoKg = pesoKg,
            Tamano = TamanoPaquete.Pequeno,
            Descripcion = "Caja",
            MontoOfrecido = pago,
            Estado = estado
        };
        db.Envios.Add(envio);
        return envio;
    }

    private static void AgregarAsignado(
        RuteaYaDbContext db,
        Viaje viaje,
        int envioId,
        DateOnly fecha,
        decimal pesoKg,
        decimal desvioKm,
        EstadoEnvio estado = EstadoEnvio.ConductorAsignado)
    {
        AgregarEnvio(db, envioId, destino: "Cerca", fecha: fecha, pesoKg: pesoKg, estado: estado, usuarioId: OtroUsuario);
        db.Asignaciones.Add(new Asignacion { Viaje = viaje, EnvioId = envioId, DesvioKm = desvioKm, Ganancia = 1000m });
    }

    /// <summary>Ruta habitual de 10 km (15 min); el desvío depende del destino del paquete. 1.5 min por km extra.</summary>
    private sealed class RutasFalsas : IRoutingService
    {
        private static readonly Dictionary<string, double> DesvioPorDestino = new()
        {
            ["En el camino"] = 0,
            ["Al lado"] = 1,
            ["A"] = 2,
            ["Cerca"] = 3,
            ["Lejos"] = 4,
            ["Borde"] = 4.5,
            ["MuyLejos"] = 6
        };

        public Task<ResultadoRutaDto> CalcularRutaAsync(string origen, string destino) =>
            throw new NotSupportedException();

        public Task<ResultadoRutaDto> CalcularRutaConParadasAsync(string origen, List<string> paradas, string destino) =>
            throw new NotSupportedException();

        public Task<CalculoDesvioDto> CalcularDesvioAsync(
            string origenViaje,
            List<string> paradasViaje,
            string destinoViaje,
            string origenPaquete,
            string destinoPaquete)
        {
            var desvio = DesvioPorDestino[destinoPaquete];

            return Task.FromResult(new CalculoDesvioDto
            {
                DistanciaHabitualKm = 10,
                DuracionHabitual = TimeSpan.FromMinutes(15),
                DistanciaConEnvioKm = 10 + desvio,
                DuracionConEnvio = TimeSpan.FromMinutes(15 + desvio * 1.5),
                RecorridoFinal = [origenViaje, origenPaquete, destinoPaquete, destinoViaje]
            });
        }
    }

    private sealed class RelojFijo(DateOnly hoy) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(hoy.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
