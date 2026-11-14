using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Servicios;
using Xunit;

namespace RuteaYa.Tests;

/// <summary>HU-18: el conductor actualiza el estado de los paquetes que aceptó llevar.</summary>
public class TransporteServiceTests
{
    private const int ConductorId = 10;
    private const int RemitenteId = 20;
    private const int OtroUsuarioId = 30;
    private const int AsignacionId = 1;

    private static RuteaYaDbContext CrearDbContext()
    {
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RuteaYaDbContext(options);
    }

    private static TransporteService CrearServicio(RuteaYaDbContext db) =>
        new(db, new EstadoEnvioService());

    /// <summary>Un viaje del conductor con un envío del remitente ya asignado, en el estado indicado.</summary>
    private static RuteaYaDbContext CrearEscenario(EstadoEnvio estado, int asignacionId = AsignacionId)
    {
        var db = CrearDbContext();
        var viaje = new Viaje { Id = 1, UsuarioId = ConductorId, Origen = "Moron", Destino = "Castelar" };
        var envio = new Envio
        {
            Id = 100,
            UsuarioId = RemitenteId,
            Origen = "Moron",
            Destino = "Haedo",
            Descripcion = "Caja chica",
            PesoKg = 3m,
            MontoOfrecido = 2500m,
            Estado = estado
        };
        db.Asignaciones.Add(new Asignacion
        {
            Id = asignacionId,
            Viaje = viaje,
            Envio = envio,
            FechaAsignacion = DateTime.UtcNow,
            Ganancia = 2500m
        });
        db.SaveChanges();
        return db;
    }

    // ---------- Transiciones válidas ----------

    [Fact]
    public async Task MarcarRetirado_DesdeConductorAsignado_PasaARetirado()
    {
        using var db = CrearEscenario(EstadoEnvio.ConductorAsignado);

        var resultado = await CrearServicio(db).MarcarRetiradoAsync(AsignacionId, ConductorId);

        Assert.Equal(EstadoEnvio.Retirado, resultado.Estado);
        Assert.Equal(EstadoEnvio.Retirado, db.Envios.Single().Estado);
    }

    [Fact]
    public async Task MarcarEnCamino_DesdeRetirado_PasaAEnCamino()
    {
        using var db = CrearEscenario(EstadoEnvio.Retirado);

        var resultado = await CrearServicio(db).MarcarEnCaminoAsync(AsignacionId, ConductorId);

        Assert.Equal(EstadoEnvio.EnCamino, resultado.Estado);
        Assert.Equal(EstadoEnvio.EnCamino, db.Envios.Single().Estado);
    }

    [Fact]
    public async Task MarcarEntregado_DesdeEnCamino_PasaAEntregado()
    {
        using var db = CrearEscenario(EstadoEnvio.EnCamino);

        var resultado = await CrearServicio(db).MarcarEntregadoAsync(AsignacionId, ConductorId);

        Assert.Equal(EstadoEnvio.Entregado, resultado.Estado);
        Assert.Equal(EstadoEnvio.Entregado, db.Envios.Single().Estado);
    }

    [Fact]
    public async Task FlujoCompleto_RetirarEnCaminoEntregar()
    {
        using var db = CrearEscenario(EstadoEnvio.ConductorAsignado);
        var servicio = CrearServicio(db);

        await servicio.MarcarRetiradoAsync(AsignacionId, ConductorId);
        await servicio.MarcarEnCaminoAsync(AsignacionId, ConductorId);
        var resultado = await servicio.MarcarEntregadoAsync(AsignacionId, ConductorId);

        Assert.Equal(EstadoEnvio.Entregado, resultado.Estado);
    }

    // ---------- Transiciones inválidas (409) ----------

    [Fact]
    public async Task MarcarEntregado_SinHaberRetirado_LanzaTransicionInvalida()
    {
        using var db = CrearEscenario(EstadoEnvio.ConductorAsignado);

        var ex = await Assert.ThrowsAsync<TransicionEstadoInvalidaException>(
            () => CrearServicio(db).MarcarEntregadoAsync(AsignacionId, ConductorId));

        Assert.Equal(EstadoEnvio.ConductorAsignado, ex.EstadoActual);
        Assert.Equal(EstadoEnvio.Entregado, ex.EstadoNuevo);
        Assert.Equal(EstadoEnvio.ConductorAsignado, db.Envios.Single().Estado);
    }

    [Fact]
    public async Task MarcarEntregado_DesdeRetiradoSinPasarPorEnCamino_LanzaTransicionInvalida()
    {
        using var db = CrearEscenario(EstadoEnvio.Retirado);

        await Assert.ThrowsAsync<TransicionEstadoInvalidaException>(
            () => CrearServicio(db).MarcarEntregadoAsync(AsignacionId, ConductorId));
    }

    [Fact]
    public async Task MarcarEnCamino_SinHaberRetirado_LanzaTransicionInvalida()
    {
        using var db = CrearEscenario(EstadoEnvio.ConductorAsignado);

        await Assert.ThrowsAsync<TransicionEstadoInvalidaException>(
            () => CrearServicio(db).MarcarEnCaminoAsync(AsignacionId, ConductorId));
    }

    [Fact]
    public async Task MarcarRetirado_DosVeces_LanzaTransicionInvalida()
    {
        using var db = CrearEscenario(EstadoEnvio.Retirado);

        await Assert.ThrowsAsync<TransicionEstadoInvalidaException>(
            () => CrearServicio(db).MarcarRetiradoAsync(AsignacionId, ConductorId));
    }

    [Theory]
    [InlineData(EstadoEnvio.Entregado)]
    [InlineData(EstadoEnvio.Cancelado)]
    public async Task EstadosFinales_NoPermitenNingunaAccion(EstadoEnvio estadoFinal)
    {
        using var db = CrearEscenario(estadoFinal);
        var servicio = CrearServicio(db);

        await Assert.ThrowsAsync<TransicionEstadoInvalidaException>(() => servicio.MarcarRetiradoAsync(AsignacionId, ConductorId));
        await Assert.ThrowsAsync<TransicionEstadoInvalidaException>(() => servicio.MarcarEnCaminoAsync(AsignacionId, ConductorId));
        await Assert.ThrowsAsync<TransicionEstadoInvalidaException>(() => servicio.MarcarEntregadoAsync(AsignacionId, ConductorId));
    }

    // ---------- Usuario no autorizado (403) y no encontrado (404) ----------

    [Theory]
    [InlineData(RemitenteId)]
    [InlineData(OtroUsuarioId)]
    public async Task CambiarEstado_UsuarioQueNoEsElConductor_LanzaAccesoDenegado(int usuarioId)
    {
        using var db = CrearEscenario(EstadoEnvio.ConductorAsignado);
        var servicio = CrearServicio(db);

        await Assert.ThrowsAsync<AccesoDenegadoException>(() => servicio.MarcarRetiradoAsync(AsignacionId, usuarioId));
        await Assert.ThrowsAsync<AccesoDenegadoException>(() => servicio.MarcarEnCaminoAsync(AsignacionId, usuarioId));
        await Assert.ThrowsAsync<AccesoDenegadoException>(() => servicio.MarcarEntregadoAsync(AsignacionId, usuarioId));
        Assert.Equal(EstadoEnvio.ConductorAsignado, db.Envios.Single().Estado);
    }

    [Fact]
    public async Task ObtenerDetalle_UsuarioQueNoEsElConductor_LanzaAccesoDenegado()
    {
        using var db = CrearEscenario(EstadoEnvio.ConductorAsignado);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => CrearServicio(db).ObtenerDetalleAsync(AsignacionId, RemitenteId));
    }

    [Fact]
    public async Task CambiarEstado_AsignacionInexistente_LanzaNotFound()
    {
        using var db = CrearEscenario(EstadoEnvio.ConductorAsignado);

        await Assert.ThrowsAsync<NotFoundException>(
            () => CrearServicio(db).MarcarRetiradoAsync(999, ConductorId));
    }

    // ---------- Listado y acciones válidas ----------

    [Fact]
    public async Task ObtenerMisTransportes_DevuelveSoloLosDelConductor()
    {
        using var db = CrearEscenario(EstadoEnvio.ConductorAsignado);

        var delConductor = await CrearServicio(db).ObtenerMisTransportesAsync(ConductorId);
        var delRemitente = await CrearServicio(db).ObtenerMisTransportesAsync(RemitenteId);

        var transporte = Assert.Single(delConductor);
        Assert.Equal(AsignacionId, transporte.AsignacionId);
        Assert.Equal(1, transporte.ViajeId);
        Assert.Equal(100, transporte.EnvioId);
        Assert.Equal("Moron", transporte.Origen);
        Assert.Equal("Haedo", transporte.Destino);
        Assert.Equal(2500m, transporte.Ganancia);
        Assert.Empty(delRemitente);
    }

    [Theory]
    [InlineData(EstadoEnvio.ConductorAsignado, true, false, false)]
    [InlineData(EstadoEnvio.Retirado, false, true, false)]
    [InlineData(EstadoEnvio.EnCamino, false, false, true)]
    [InlineData(EstadoEnvio.Entregado, false, false, false)]
    [InlineData(EstadoEnvio.Cancelado, false, false, false)]
    public async Task ObtenerMisTransportes_SoloHabilitaLasAccionesValidasParaElEstado(
        EstadoEnvio estado, bool retirar, bool enCamino, bool entregar)
    {
        using var db = CrearEscenario(estado);

        var transporte = Assert.Single(await CrearServicio(db).ObtenerMisTransportesAsync(ConductorId));

        Assert.Equal(retirar, transporte.PuedeMarcarRetirado);
        Assert.Equal(enCamino, transporte.PuedeMarcarEnCamino);
        Assert.Equal(entregar, transporte.PuedeMarcarEntregado);
    }
}
