using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos.Envios;
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

    [Fact]
    public async Task CrearEnvioAsync_DatosValidos_DebeCrearEnvioConEstadoBuscandoConductor()
    {
        
        using var context = CrearContextoEnMemoria();
        var estadoService = new EstadoEnvioService();
        var envioService = new EnvioService(context, estadoService);

        int usuarioId = 10;
        var dto = new CrearEnvioDto
        {
            Origen = "Ituzaingó",
            Destino = "Morón",
            Fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            PesoKg = 4.5m,
            Tamano = TamanoPaquete.Mediano,
            Descripcion = "Caja con repuestos mecánicos",
            MontoOfrecido = 3500m
        };

        
        var resultado = await envioService.CrearEnvioAsync(dto, usuarioId);

        Assert.NotNull(resultado);
        Assert.True(resultado.Id > 0);
        Assert.Equal("Ituzaingó", resultado.Origen);
        Assert.Equal("Morón", resultado.Destino);
        Assert.Equal(EstadoEnvio.BuscandoConductor, resultado.Estado);

        
        var envioEnDb = await context.Envios.FindAsync(resultado.Id);
        Assert.NotNull(envioEnDb);
        Assert.Equal(usuarioId, envioEnDb.UsuarioId);
        Assert.Equal(EstadoEnvio.BuscandoConductor, envioEnDb.Estado);

    }

    [Theory]
    [InlineData("Morón", "Morón")]
    [InlineData("Ituzaingó", "ituzaingó")]
    [InlineData("  Haedo ", "haedo")]
    public async Task CrearEnvioAsync_OrigenYDestinoIguales_DebeLanzarArgumentException(string origen, string destino)
    {
        
        using var context = CrearContextoEnMemoria();
        var estadoService = new EstadoEnvioService();
        var envioService = new EnvioService(context, estadoService);

        var dto = new CrearEnvioDto
        {
            Origen = origen,
            Destino = destino,
            Fecha = DateOnly.FromDateTime(DateTime.Today),
            PesoKg = 2m,
            Tamano = TamanoPaquete.Pequeno,
            Descripcion = "Documentación",
            MontoOfrecido = 1000m
        };

        
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            envioService.CrearEnvioAsync(dto, usuarioId: 1)
        );
        Assert.Equal("El origen y el destino no pueden ser iguales.", ex.Message);
    }

    [Fact]
    public async Task CrearEnvioAsync_FechaAnteriorAHoy_DebeLanzarArgumentException()
    {
        
        using var context = CrearContextoEnMemoria();
        var estadoService = new EstadoEnvioService();
        var envioService = new EnvioService(context, estadoService);

        var dto = new CrearEnvioDto
        {
            Origen = "Ituzaingó",
            Destino = "Castelar",
            Fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), // Ayer
            PesoKg = 1m,
            Tamano = TamanoPaquete.Pequeno,
            Descripcion = "Sobre",
            MontoOfrecido = 1500m
        };

        
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            envioService.CrearEnvioAsync(dto, usuarioId: 1)
        );
        Assert.Equal("La fecha no puede ser anterior a la actual.", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2.5)]
    public async Task CrearEnvioAsync_PesoInvalido_DebeLanzarArgumentException(decimal pesoInvalido)
    {
        // Arrange
        using var context = CrearContextoEnMemoria();
        var estadoService = new EstadoEnvioService();
        var envioService = new EnvioService(context, estadoService);

        var dto = new CrearEnvioDto
        {
            Origen = "Ituzaingó",
            Destino = "Castelar",
            Fecha = DateOnly.FromDateTime(DateTime.Today),
            PesoKg = pesoInvalido,
            Tamano = TamanoPaquete.Pequeno,
            Descripcion = "Paquete liviano",
            MontoOfrecido = 1500m
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            envioService.CrearEnvioAsync(dto, usuarioId: 1)
        );
        Assert.Equal("El peso debe ser mayor a 0.", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public async Task CrearEnvioAsync_MontoOfrecidoInvalido_DebeLanzarArgumentException(decimal montoInvalido)
    {
        // Arrange
        using var context = CrearContextoEnMemoria();
        var estadoService = new EstadoEnvioService();
        var envioService = new EnvioService(context, estadoService);

        var dto = new CrearEnvioDto
        {
            Origen = "Ituzaingó",
            Destino = "Castelar",
            Fecha = DateOnly.FromDateTime(DateTime.Today),
            PesoKg = 2m,
            Tamano = TamanoPaquete.Pequeno,
            Descripcion = "Paquete",
            MontoOfrecido = montoInvalido
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            envioService.CrearEnvioAsync(dto, usuarioId: 1)
        );
        Assert.Equal("El monto ofrecido debe ser mayor a 0.", ex.Message);
    }
}
