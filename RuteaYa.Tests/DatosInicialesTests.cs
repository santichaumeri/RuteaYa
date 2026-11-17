using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos;
using RuteaYa.Logica.Dtos.Historial;
using RuteaYa.Logica.Dtos.Oportunidades;
using RuteaYa.Logica.Oportunidades;
using RuteaYa.Logica.Servicios;
using RuteaYa.Logica.Servicios.Rutas;
using Xunit;

namespace RuteaYa.Tests;

/// <summary>
/// Los datos iniciales (HU-22) tienen que servir para probar el sistema y respetar las reglas de negocio:
/// estos tests usan los servicios reales con el mock de rutas, igual que la API en desarrollo.
/// </summary>
public sealed class DatosInicialesTests
{
    /// <summary>Lunes 16 de noviembre de 2026.</summary>
    private static readonly RelojFijo Reloj = new(new DateOnly(2026, 11, 16));

    [Fact]
    public async Task Carga_los_datos_una_sola_vez()
    {
        await using var db = CrearDbContext();

        Assert.True(await CargarAsync(db));
        var envios = await db.Envios.CountAsync();
        Assert.False(await CargarAsync(db));

        Assert.Equal(DatosIniciales.Emails.Count, await db.Usuarios.CountAsync());
        Assert.Equal(7, await db.Viajes.CountAsync());
        Assert.Equal(envios, await db.Envios.CountAsync());
    }

    [Fact]
    public async Task Hay_volumen_suficiente_para_que_las_pantallas_muestren_datos()
    {
        await using var db = CrearDbContext();
        await CargarAsync(db);

        Assert.True(await db.Envios.CountAsync(x => x.Estado == EstadoEnvio.BuscandoConductor) >= 40);
        Assert.True(await db.Envios.CountAsync(x => x.Estado == EstadoEnvio.Entregado) >= 100);
        Assert.True(await db.Envios.CountAsync(x => x.Estado == EstadoEnvio.Cancelado) >= 5);
    }

    [Fact]
    public async Task Genera_siempre_los_mismos_datos_para_el_mismo_dia()
    {
        await using var primera = CrearDbContext();
        await using var segunda = CrearDbContext();
        await CargarAsync(primera);
        await CargarAsync(segunda);

        Assert.Equal(await ResumenAsync(primera), await ResumenAsync(segunda));

        static async Task<List<string>> ResumenAsync(RuteaYaDbContext db) => await db.Envios
            .OrderBy(x => x.Id)
            .Select(x => $"{x.Origen}|{x.Destino}|{x.Fecha}|{x.PesoKg}|{x.MontoOfrecido}|{x.Estado}|{x.Descripcion}")
            .ToListAsync();
    }

    [Theory]
    [MemberData(nameof(TodosLosEmails))]
    public async Task Los_usuarios_de_prueba_inician_sesion_con_la_contrasena_documentada(string email)
    {
        await using var db = CrearDbContext();
        await CargarAsync(db);
        var usuarios = new UsuarioService(db, new PasswordHasher<Usuario>());

        var usuario = await usuarios.LoginAsync(
            new LoginRequest { Email = email, Password = DatosIniciales.PasswordDePrueba },
            CancellationToken.None);

        Assert.Equal(email, usuario.Email);
    }

    public static TheoryData<string> TodosLosEmails() => new(DatosIniciales.Emails);

    [Fact]
    public async Task Las_asignaciones_respetan_la_capacidad_y_el_desvio_de_cada_viaje_por_fecha()
    {
        await using var db = CrearDbContext();
        await CargarAsync(db);

        var porViajeYFecha = await db.Asignaciones
            .Include(x => x.Viaje)
            .Include(x => x.Envio)
            .ToListAsync();

        foreach (var grupo in porViajeYFecha.GroupBy(x => (x.ViajeId, x.Envio.Fecha)))
        {
            var viaje = grupo.First().Viaje;
            Assert.True(grupo.Count() <= viaje.MaxPaquetes, $"Viaje {viaje.Id} el {grupo.Key.Fecha}: demasiados paquetes.");
            Assert.True(grupo.Sum(x => x.Envio.PesoKg) <= viaje.PesoMaximoKg, $"Viaje {viaje.Id} el {grupo.Key.Fecha}: demasiado peso.");
            Assert.True(grupo.Sum(x => x.DesvioKm) <= viaje.DesvioMaximoKm, $"Viaje {viaje.Id} el {grupo.Key.Fecha}: demasiado desvío.");
        }

        // Nadie lleva sus propios envíos y cada asignación cae en un día que el viaje sale.
        Assert.All(porViajeYFecha, x => Assert.NotEqual(x.Viaje.UsuarioId, x.Envio.UsuarioId));
        Assert.All(porViajeYFecha, x => Assert.True(ReglasCompatibilidad.SaleEl(x.Viaje, x.Envio.Fecha)));
    }

    [Fact]
    public async Task El_desvio_guardado_en_cada_asignacion_es_el_que_calcula_el_mock()
    {
        await using var db = CrearDbContext();
        await CargarAsync(db);
        var rutas = new MockRoutingService();

        var asignaciones = await db.Asignaciones
            .Include(x => x.Envio)
            .Include(x => x.Viaje).ThenInclude(x => x.Paradas)
            .ToListAsync();

        foreach (var asignacion in asignaciones)
        {
            var viaje = asignacion.Viaje;
            var calculo = await rutas.CalcularDesvioAsync(
                viaje.Origen,
                viaje.Paradas.OrderBy(x => x.Orden).Select(x => x.Direccion).ToList(),
                viaje.Destino,
                asignacion.Envio.Origen,
                asignacion.Envio.Destino);

            Assert.Equal(
                Math.Round((decimal)(calculo.DistanciaConEnvioKm - calculo.DistanciaHabitualKm), 2),
                asignacion.DesvioKm);
        }
    }

    [Fact]
    public async Task El_viaje_semanal_de_Carla_muestra_los_ejemplos_del_README()
    {
        await using var db = CrearDbContext();
        await CargarAsync(db);
        var (conductora, semanal, _) = await ViajesDeCarlaAsync(db);

        var oportunidades = await CrearOportunidades(db)
            .BuscarAsync(semanal, conductora, OrdenOportunidades.Ganancia, CancellationToken.None);
        var nivel = oportunidades
            .GroupBy(x => x.Descripcion)
            .ToDictionary(x => x.Key, x => x.First().NivelCompatibilidad);

        Assert.Equal(NivelCompatibilidad.MuyCompatible, nivel["Documentación para firmar"]);
        Assert.Equal(NivelCompatibilidad.Compatible, nivel["Caja con repuestos"]);
        Assert.Equal(NivelCompatibilidad.MuchoDesvio, nivel["Juego de sábanas"]);
        Assert.Contains("Microondas", nivel.Keys);
        Assert.Contains("Ropa", nivel.Keys);

        // "Libros" supera el desvío máximo y "Bicicleta" el peso máximo: no se ofrecen.
        Assert.DoesNotContain("Libros", nivel.Keys);
        Assert.DoesNotContain("Bicicleta", nivel.Keys);
    }

    [Fact]
    public async Task El_lunes_hay_mas_carga_compatible_de_la_que_entra_en_el_viaje()
    {
        await using var db = CrearDbContext();
        await CargarAsync(db);
        var (conductora, semanal, _) = await ViajesDeCarlaAsync(db);

        var candidatos = await CrearOportunidades(db)
            .ObtenerCandidatosPorFechaAsync(semanal, conductora, CancellationToken.None);
        var lunes = candidatos.Fechas.Single(x => x.Fecha == new DateOnly(2026, 11, 23));
        var viaje = await db.Viajes.SingleAsync(x => x.Id == semanal);

        // Así el optimizador tiene que elegir qué envíos llevar.
        Assert.True(lunes.Oportunidades.Count > viaje.MaxPaquetes);
        Assert.True(lunes.Oportunidades.Sum(x => x.PesoKg) > viaje.PesoMaximoKg);
    }

    [Fact]
    public async Task Todos_los_viajes_activos_tienen_oportunidades_y_el_inactivo_no()
    {
        await using var db = CrearDbContext();
        await CargarAsync(db);
        var servicio = CrearOportunidades(db);

        foreach (var viaje in await db.Viajes.Where(x => x.Estado == EstadoViaje.Activo).ToListAsync())
        {
            var oportunidades = await servicio.BuscarAsync(viaje.Id, viaje.UsuarioId, OrdenOportunidades.Ganancia, CancellationToken.None);
            Assert.True(oportunidades.Count >= 3, $"El viaje {viaje.Origen} → {viaje.Destino} tiene {oportunidades.Count} oportunidades.");
        }

        Assert.Single(await db.Viajes.Where(x => x.Estado == EstadoViaje.Inactivo).ToListAsync());
    }

    [Fact]
    public async Task El_viaje_del_sabado_ofrece_los_ejemplos_del_README()
    {
        await using var db = CrearDbContext();
        await CargarAsync(db);
        var (conductora, _, sabado) = await ViajesDeCarlaAsync(db);

        var oportunidades = await CrearOportunidades(db)
            .BuscarAsync(sabado, conductora, OrdenOportunidades.Ganancia, CancellationToken.None);

        Assert.Contains(oportunidades, x => x.Descripcion == "Regalo de cumpleaños");
        Assert.Contains(oportunidades, x => x.Descripcion == "Plantas");
    }

    [Fact]
    public async Task Cada_conductor_tiene_historial_con_ganancias_efectivas()
    {
        await using var db = CrearDbContext();
        await CargarAsync(db);
        var historial = new HistorialService(db);

        foreach (var email in new[] { DatosIniciales.EmailConductora, DatosIniciales.EmailConductor2, DatosIniciales.EmailConductora3 })
        {
            var conductor = await db.Usuarios.SingleAsync(x => x.Email == email);
            var entregadas = await db.Asignaciones
                .Where(x => x.Viaje.UsuarioId == conductor.Id && x.Envio.Estado == EstadoEnvio.Entregado)
                .ToListAsync();

            var resumen = await historial.ObtenerHistorialAsync(conductor.Id, new HistorialFiltroDto());

            Assert.True(entregadas.Count >= 15, $"{email} tiene {entregadas.Count} entregas.");
            Assert.Equal(entregadas.Count, resumen.EnviosTransportados);
            Assert.Equal(entregadas.Sum(x => x.Ganancia), resumen.GananciaEfectiva);
        }
    }

    [Fact]
    public async Task Hay_envios_en_todos_los_estados()
    {
        await using var db = CrearDbContext();
        await CargarAsync(db);

        var estados = await db.Envios.Select(x => x.Estado).Distinct().ToListAsync();

        Assert.Equal(Enum.GetValues<EstadoEnvio>(), estados.Order());
    }

    private static Task<bool> CargarAsync(RuteaYaDbContext db) =>
        DatosIniciales.CargarAsync(db, new PasswordHasher<Usuario>(), Reloj);

    private static OportunidadService CrearOportunidades(RuteaYaDbContext db) =>
        new(db, new MockRoutingService(), Reloj);

    private static async Task<(int Conductora, int Semanal, int Sabado)> ViajesDeCarlaAsync(RuteaYaDbContext db)
    {
        var conductora = await db.Usuarios.SingleAsync(x => x.Email == DatosIniciales.EmailConductora);
        var viajes = await db.Viajes.Where(x => x.UsuarioId == conductora.Id && x.Estado == EstadoViaje.Activo).ToListAsync();

        return (
            conductora.Id,
            viajes.Single(x => x.Destino == "CABA").Id,
            viajes.Single(x => x.Destino == "Merlo").Id);
    }

    private static RuteaYaDbContext CrearDbContext()
    {
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new RuteaYaDbContext(options);
    }
}
