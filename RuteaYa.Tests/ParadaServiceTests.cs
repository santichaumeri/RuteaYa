using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos;
using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Servicios;
using Xunit;

namespace RuteaYa.Tests;

/// <summary>Endpoints de paradas dentro de un viaje (api/viajes/{id}/paradas).</summary>
public sealed class ParadaServiceTests
{
    private const int Conductor = 7;

    [Fact]
    public async Task Crear_sin_orden_agrega_la_parada_al_final()
    {
        await using var db = CrearDbContext();
        var viaje = await CrearViajeAsync(db, "Haedo", "Ramos Mejia");

        var creada = await Servicio(db).CrearAsync(viaje, Conductor, new ParadaRequest { Direccion = " San Justo " }, default);

        Assert.Equal("San Justo", creada.Direccion);
        Assert.Equal(3, creada.Orden);
        Assert.Equal(["Haedo", "Ramos Mejia", "San Justo"], await DireccionesAsync(db, viaje));
    }

    [Fact]
    public async Task Crear_con_orden_inserta_la_parada_y_corre_las_siguientes()
    {
        await using var db = CrearDbContext();
        var viaje = await CrearViajeAsync(db, "Haedo", "Ramos Mejia");

        var creada = await Servicio(db).CrearAsync(viaje, Conductor, new ParadaRequest { Direccion = "Castelar", Orden = 1 }, default);

        Assert.Equal(1, creada.Orden);
        Assert.Equal(["Castelar", "Haedo", "Ramos Mejia"], await DireccionesAsync(db, viaje));
        Assert.Equal([1, 2, 3], await db.Paradas.Where(x => x.ViajeId == viaje).OrderBy(x => x.Orden).Select(x => x.Orden).ToListAsync());
    }

    [Theory]
    [InlineData("haedo", "No se pueden repetir direcciones de paradas.")]
    [InlineData("Moron", "Una parada no puede coincidir con el origen ni con el destino.")]
    [InlineData("  ", "La dirección de cada parada es obligatoria.")]
    public async Task Crear_aplica_las_mismas_reglas_que_el_alta_del_viaje(string direccion, string mensaje)
    {
        await using var db = CrearDbContext();
        var viaje = await CrearViajeAsync(db, "Haedo");

        var error = await Assert.ThrowsAsync<ValidacionException>(() =>
            Servicio(db).CrearAsync(viaje, Conductor, new ParadaRequest { Direccion = direccion }, default));

        Assert.Equal(mensaje, error.Message);
        Assert.Equal(["Haedo"], await DireccionesAsync(db, viaje));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public async Task Crear_rechaza_un_orden_fuera_del_recorrido(int orden)
    {
        await using var db = CrearDbContext();
        var viaje = await CrearViajeAsync(db, "Haedo", "Ramos Mejia");

        var error = await Assert.ThrowsAsync<ValidacionException>(() =>
            Servicio(db).CrearAsync(viaje, Conductor, new ParadaRequest { Direccion = "Castelar", Orden = orden }, default));

        Assert.Equal("El orden de la parada tiene que estar entre 1 y 3.", error.Message);
    }

    [Fact]
    public async Task Modificar_cambia_la_direccion_y_mueve_la_parada()
    {
        await using var db = CrearDbContext();
        var viaje = await CrearViajeAsync(db, "Haedo", "Ramos Mejia", "San Justo");
        var haedo = await db.Paradas.SingleAsync(x => x.Direccion == "Haedo");

        var modificada = await Servicio(db).ModificarAsync(
            viaje, haedo.Id, Conductor, new ParadaRequest { Direccion = "Castelar", Orden = 3 }, default);

        Assert.Equal(3, modificada.Orden);
        Assert.Equal(["Ramos Mejia", "San Justo", "Castelar"], await DireccionesAsync(db, viaje));
    }

    [Fact]
    public async Task Modificar_sin_orden_deja_la_parada_en_su_lugar()
    {
        await using var db = CrearDbContext();
        var viaje = await CrearViajeAsync(db, "Haedo", "Ramos Mejia");
        var ramos = await db.Paradas.SingleAsync(x => x.Direccion == "Ramos Mejia");

        await Servicio(db).ModificarAsync(viaje, ramos.Id, Conductor, new ParadaRequest { Direccion = "Ciudadela" }, default);

        Assert.Equal(["Haedo", "Ciudadela"], await DireccionesAsync(db, viaje));
    }

    [Fact]
    public async Task Eliminar_renumera_las_paradas_que_quedan()
    {
        await using var db = CrearDbContext();
        var viaje = await CrearViajeAsync(db, "Haedo", "Ramos Mejia", "San Justo");
        var haedo = await db.Paradas.SingleAsync(x => x.Direccion == "Haedo");

        await Servicio(db).EliminarAsync(viaje, haedo.Id, Conductor, default);

        var quedan = await db.Paradas.Where(x => x.ViajeId == viaje).OrderBy(x => x.Orden).ToListAsync();
        Assert.Equal(["Ramos Mejia", "San Justo"], quedan.Select(x => x.Direccion));
        Assert.Equal([1, 2], quedan.Select(x => x.Orden));
    }

    [Fact]
    public async Task Solo_el_duenio_del_viaje_puede_tocar_sus_paradas()
    {
        await using var db = CrearDbContext();
        var viaje = await CrearViajeAsync(db, "Haedo");
        var parada = await db.Paradas.SingleAsync();
        var servicio = Servicio(db);

        await Assert.ThrowsAsync<AccesoDenegadoException>(() => servicio.ObtenerAsync(viaje, 99, default));
        await Assert.ThrowsAsync<AccesoDenegadoException>(() =>
            servicio.CrearAsync(viaje, 99, new ParadaRequest { Direccion = "Castelar" }, default));
        await Assert.ThrowsAsync<AccesoDenegadoException>(() =>
            servicio.ModificarAsync(viaje, parada.Id, 99, new ParadaRequest { Direccion = "Castelar" }, default));
        await Assert.ThrowsAsync<AccesoDenegadoException>(() => servicio.EliminarAsync(viaje, parada.Id, 99, default));
        Assert.Equal(["Haedo"], await DireccionesAsync(db, viaje));
    }

    [Fact]
    public async Task Una_parada_de_otro_viaje_o_un_viaje_inexistente_dan_404()
    {
        await using var db = CrearDbContext();
        var viaje = await CrearViajeAsync(db, "Haedo");
        var otroViaje = await CrearViajeAsync(db, "Castelar");
        var paradaAjena = await db.Paradas.SingleAsync(x => x.ViajeId == otroViaje);
        var servicio = Servicio(db);

        await Assert.ThrowsAsync<NotFoundException>(() => servicio.EliminarAsync(viaje, paradaAjena.Id, Conductor, default));
        await Assert.ThrowsAsync<NotFoundException>(() => servicio.ObtenerAsync(999, Conductor, default));
    }

    [Fact]
    public async Task Obtener_devuelve_las_paradas_en_el_orden_del_recorrido()
    {
        await using var db = CrearDbContext();
        var viaje = await CrearViajeAsync(db, "Haedo", "Ramos Mejia");

        var paradas = await Servicio(db).ObtenerAsync(viaje, Conductor, default);

        Assert.Equal(["Haedo", "Ramos Mejia"], paradas.Select(x => x.Direccion));
        Assert.Equal([1, 2], paradas.Select(x => x.Orden));
    }

    private static ParadaService Servicio(RuteaYaDbContext db) => new(db);

    /// <summary>Viaje Moron → CABA del conductor con las paradas indicadas, en orden.</summary>
    private static async Task<int> CrearViajeAsync(RuteaYaDbContext db, params string[] paradas)
    {
        var viaje = new Viaje
        {
            UsuarioId = Conductor,
            Origen = "Moron",
            Destino = "CABA",
            DiasSemana = DiasSemana.Lunes,
            MaxPaquetes = 2,
            PesoMaximoKg = 10,
            DesvioMaximoKm = 5,
            Estado = EstadoViaje.Activo,
            Paradas = paradas.Select((direccion, indice) => new Parada { Direccion = direccion, Orden = indice + 1 }).ToList()
        };
        db.Viajes.Add(viaje);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return viaje.Id;
    }

    private static async Task<List<string>> DireccionesAsync(RuteaYaDbContext db, int viajeId) =>
        await db.Paradas.AsNoTracking().Where(x => x.ViajeId == viajeId).OrderBy(x => x.Orden).Select(x => x.Direccion).ToListAsync();

    private static RuteaYaDbContext CrearDbContext() =>
        new(new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);
}
