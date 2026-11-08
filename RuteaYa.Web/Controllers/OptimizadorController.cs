using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Web.Models.Optimizador;
using RuteaYa.Web.Servicios;

namespace RuteaYa.Web.Controllers;

/// <summary>Pantalla 10: Optimizador de oportunidades (HU-14). El cálculo lo hace la API.</summary>
[Authorize]
public sealed class OptimizadorController(
    IOptimizadorApiClient optimizadorApiClient,
    DashboardApiClient dashboardApiClient) : Controller
{
    /// <summary>
    /// Sin viaje (/Optimizador, link del menú) muestra el selector de viajes;
    /// con viaje calcula la combinación para la fecha y el criterio elegidos.
    /// </summary>
    [HttpGet("/Optimizador")]
    [HttpGet("/Viajes/{viajeId:int}/Optimizador")]
    public async Task<IActionResult> Index(
        int? viajeId,
        DateOnly? fecha,
        string? criterio,
        CancellationToken cancellationToken)
    {
        // El selector usa los viajes activos del dashboard hasta que exista el listado de HU-05 (GET api/viajes).
        var resumen = await dashboardApiClient.ObtenerResumenAsync(cancellationToken);
        if (resumen.EstaNoAutorizado)
        {
            return Challenge();
        }

        var modelo = new OptimizadorPaginaViewModel
        {
            MisViajes = resumen.Resumen?.Viajes.ToList() ?? [],
            Criterio = CriteriosOptimizacion.Normalizar(criterio)
        };

        if (viajeId is null)
        {
            return View(modelo);
        }

        try
        {
            modelo.Datos = await optimizadorApiClient.CalcularAsync(viajeId.Value, fecha, modelo.Criterio, cancellationToken);
        }
        catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            return Challenge();
        }
        catch (ApiException ex) when (ex.StatusCode is StatusCodes.Status400BadRequest or StatusCodes.Status409Conflict)
        {
            // Viaje inactivo o datos inválidos: se explica en la misma pantalla.
            modelo.Error = ex.Message;
        }
        catch (ApiException ex)
        {
            return StatusCode(ex.StatusCode, ex.Message);
        }

        return View(modelo);
    }
}
