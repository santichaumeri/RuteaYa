using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Servicios;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace RuteaYa.Tests;

public class EnvioServiceTest
{
    private RuteaYaDbContext CrearContextoEnMemoria()
    {
        var opciones = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(databaseName: $"RuteaYaTestDb_{Guid.NewGuid()}")
            .Options;

        return new RuteaYaDbContext(opciones);
    }

    [Fact]
    public async Task ObtenerMisEnviosAsync_DebeListarSoloLosEnviosDelUsuarioAutenticado()
    {
        using var context = CrearContextoEnMemoria();
        var estadoService = new EstadoEnvioService();
        var envioService = new EnvioService(context, estadoService);

        int usuarioActualId = 1;
        int otroUsuarioId = 2;

        context.Envios.AddRange(
            new Envio { Id = 1, UsuarioId = usuarioActualId, Origen = "Ituzaingó", Destino = "Morón", Estado = EstadoEnvio.BuscandoConductor },
            new Envio { Id = 2, UsuarioId = usuarioActualId, Origen = "Castelar", Destino = "Haedo", Estado = EstadoEnvio.ConductorAsignado },
            new Envio { Id = 3, UsuarioId = otroUsuarioId, Origen = "Rosario", Destino = "CABA", Estado = EstadoEnvio.BuscandoConductor }
        );
        await context.SaveChangesAsync();

        var resultado = (await envioService.ObtenerMisEnviosAsync(usuarioActualId)).ToList();

        Assert.Equal(2, resultado.Count);
        Assert.All(resultado, e => Assert.True(e.Id == 1 || e.Id == 2));
        Assert.DoesNotContain(resultado, e => e.Id == 3);
    }

    [Fact]
    public async Task ObtenerMisEnviosAsync_ConFiltroDeEstado_DebeRetornarSoloLosQueCoinciden()
    {
        using var context = CrearContextoEnMemoria();
        var estadoService = new EstadoEnvioService();
        var envioService = new EnvioService(context, estadoService);

        int usuarioId = 1;

        context.Envios.AddRange(
            new Envio { Id = 1, UsuarioId = usuarioId, Origen = "A", Destino = "B", Estado = EstadoEnvio.BuscandoConductor },
            new Envio { Id = 2, UsuarioId = usuarioId, Origen = "C", Destino = "D", Estado = EstadoEnvio.Entregado },
            new Envio { Id = 3, UsuarioId = usuarioId, Origen = "E", Destino = "F", Estado = EstadoEnvio.BuscandoConductor }
        );
        await context.SaveChangesAsync();

        var resultado = (await envioService.ObtenerMisEnviosAsync(usuarioId, EstadoEnvio.BuscandoConductor)).ToList();

        Assert.Equal(2, resultado.Count);
        Assert.All(resultado, e => Assert.Equal(EstadoEnvio.BuscandoConductor, e.Estado));
    }

    [Fact]
    public async Task ObtenerPorIdAsync_EnvioAjeno_DebeRetornarNull()
    {
        using var context = CrearContextoEnMemoria();
        var estadoService = new EstadoEnvioService();
        var envioService = new EnvioService(context, estadoService);

        int usuarioActualId = 1;
        int otroUsuarioId = 2;

        context.Envios.Add(new Envio 
        { 
            Id = 10, 
            UsuarioId = otroUsuarioId, 
            Origen = "A", 
            Destino = "B" 
        });

        await context.SaveChangesAsync();

        // Act
        var resultado = await envioService.ObtenerPorIdAsync(10, usuarioActualId);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task CancelarEnvioAsync_EstadoValido_DebePasarACanceladoYGuardarEnBD()
    {
        using var context = CrearContextoEnMemoria();
        var estadoService = new EstadoEnvioService();
        var envioService = new EnvioService(context, estadoService);

        int usuarioId = 1;
        context.Envios.Add(new Envio
        {
            Id = 5,
            UsuarioId = usuarioId,
            Origen = "Ituzaingó",
            Destino = "CABA",
            Estado = EstadoEnvio.BuscandoConductor
        });
        await context.SaveChangesAsync();

        await envioService.CancelarEnvioAsync(5, usuarioId);

        var envioActualizado = await context.Envios.FindAsync(5);
        Assert.NotNull(envioActualizado);
        Assert.Equal(EstadoEnvio.Cancelado, envioActualizado.Estado);
    }

    [Fact]
    public async Task CancelarEnvioAsync_EnvioEntregado_DebeLanzarTransicionEstadoInvalidaException()
    {

        using var context = CrearContextoEnMemoria();
        var estadoService = new EstadoEnvioService();
        var envioService = new EnvioService(context, estadoService);

        int usuarioId = 1;
        context.Envios.Add(new Envio
        {
            Id = 8,
            UsuarioId = usuarioId,
            Origen = "A",
            Destino = "B",
            Estado = EstadoEnvio.Entregado
        });
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<TransicionEstadoInvalidaException>(() =>
            envioService.CancelarEnvioAsync(8, usuarioId)
        );

        var envioSinCambios = await context.Envios.FindAsync(8);
        Assert.NotNull(envioSinCambios);
        Assert.Equal(EstadoEnvio.Entregado, envioSinCambios.Estado);
    }

    [Fact]
    public async Task CancelarEnvioAsync_EnvioDeOtroUsuario_DebeLanzarKeyNotFoundException()
        {
            using var context = CrearContextoEnMemoria();
            var estadoService = new EstadoEnvioService();
            var envioService = new EnvioService(context, estadoService);

            int usuarioActualId = 1;
            int otroUsuarioId = 2;

            context.Envios.Add(new Envio
            {
                Id = 20,
                UsuarioId = otroUsuarioId,
                Origen = "A",
                Destino = "B",
                Estado = EstadoEnvio.BuscandoConductor
            });
            await context.SaveChangesAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            envioService.CancelarEnvioAsync(20, usuarioActualId));
    }
}
