using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RuteaYa.Api.Controllers;
using RuteaYa.Api.Middleware;
using RuteaYa.Logica.Dtos.Envios;
using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Servicios;
using Xunit;

namespace RuteaYa.Tests;

/// <summary>
/// Errores HTTP de api/envios. Antes un token sin id de usuario terminaba en 500 porque se lanzaba
/// UnauthorizedAccessException, que el manejador global no conoce. Ahora todo pasa por excepciones de negocio.
/// </summary>
public sealed class EnviosControllerTests
{
    [Fact]
    public async Task Sin_id_de_usuario_en_el_token_lanza_NoAutenticado()
    {
        var controller = CrearController(new Mock<IEnvioService>().Object, usuarioId: null);

        await Assert.ThrowsAsync<NoAutenticadoException>(() => controller.ObtenerMisEnvios(estado: null));
    }

    [Fact]
    public async Task Envio_inexistente_o_ajeno_lanza_NotFound()
    {
        var servicio = new Mock<IEnvioService>();
        servicio.Setup(s => s.ObtenerPorIdAsync(7, 1)).ReturnsAsync((EnvioDetalleDto?)null);
        var controller = CrearController(servicio.Object, usuarioId: 1);

        await Assert.ThrowsAsync<NotFoundException>(() => controller.ObtenerPorId(7));
    }

    [Fact]
    public async Task Envio_propio_devuelve_200()
    {
        var servicio = new Mock<IEnvioService>();
        servicio.Setup(s => s.ObtenerPorIdAsync(7, 1)).ReturnsAsync(new EnvioDetalleDto { Id = 7 });
        var controller = CrearController(servicio.Object, usuarioId: 1);

        var resultado = Assert.IsType<OkObjectResult>(await controller.ObtenerPorId(7));
        Assert.Equal(7, Assert.IsType<EnvioDetalleDto>(resultado.Value).Id);
    }

    [Theory]
    [InlineData(typeof(NoAutenticadoException), StatusCodes.Status401Unauthorized)]
    [InlineData(typeof(ValidacionException), StatusCodes.Status400BadRequest)]
    [InlineData(typeof(NotFoundException), StatusCodes.Status404NotFound)]
    public async Task El_manejador_global_traduce_las_excepciones_de_envios(Type tipo, int statusEsperado)
    {
        var excepcion = (Exception)Activator.CreateInstance(tipo, "mensaje de prueba")!;
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var manejado = await new ManejadorGlobalDeExcepciones(NullLogger<ManejadorGlobalDeExcepciones>.Instance)
            .TryHandleAsync(httpContext, excepcion, CancellationToken.None);

        Assert.True(manejado);
        Assert.Equal(statusEsperado, httpContext.Response.StatusCode);

        httpContext.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(httpContext.Response.Body);
        Assert.Equal("mensaje de prueba", json.RootElement.GetProperty("detail").GetString());
    }

    private static EnviosController CrearController(IEnvioService servicio, int? usuarioId)
    {
        var claims = usuarioId is null ? [] : new[] { new Claim(ClaimTypes.NameIdentifier, usuarioId.Value.ToString()) };

        return new EnviosController(servicio)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
            }
        };
    }
}
