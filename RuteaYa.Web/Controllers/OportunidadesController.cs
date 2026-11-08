using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Web.Models.Oportunidades;
using RuteaYa.Web.Servicios;

namespace RuteaYa.Web.Controllers;

/// <summary>Pantallas 8 (Oportunidades) y 9 (Detalle de oportunidad). Toda la lógica vive en la API.</summary>
[Authorize]
public sealed class OportunidadesController(
    IOportunidadesApiClient oportunidadesApiClient,
    IViajesApiClient viajesApiClient,
    DashboardApiClient dashboardApiClient) : Controller
{
    private static readonly string[] OrdenesValidos = ["Ganancia", "Desvio", "Compatibilidad"];

    /// <summary>
    /// Sin viaje (/Oportunidades, link del menú) muestra el selector de viajes;
    /// con viaje (/Viajes/{id}/Oportunidades) además lista los envíos compatibles.
    /// </summary>
    [HttpGet("/Oportunidades")]
    [HttpGet("/Viajes/{viajeId:int}/Oportunidades")]
    public async Task<IActionResult> Index(
        int? viajeId,
        string? orden,
        CancellationToken cancellationToken)
    {
        var ordenElegido = OrdenesValidos.FirstOrDefault(
            valido => string.Equals(valido, orden, StringComparison.OrdinalIgnoreCase)) ?? "Ganancia";

        // El selector usa los viajes activos del dashboard hasta que exista el listado de HU-05 (GET api/viajes).
        var resumen = await dashboardApiClient.ObtenerResumenAsync(cancellationToken);
        if (resumen.EstaNoAutorizado)
        {
            return Challenge();
        }

        var modelo = new OportunidadesViewModel
        {
            MisViajes = resumen.Resumen?.Viajes.ToList() ?? [],
            Orden = ordenElegido
        };

        if (viajeId is null)
        {
            return View(modelo);
        }

        try
        {
            modelo.Viaje = await viajesApiClient.ObtenerDetalleAsync(viajeId.Value, cancellationToken);

            try
            {
                modelo.Oportunidades = await oportunidadesApiClient.BuscarAsync(viajeId.Value, ordenElegido, cancellationToken);
            }
            catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status409Conflict)
            {
                // Viaje inactivo: se muestra el motivo en la misma pantalla.
                modelo.Error = ex.Message;
            }

            return View(modelo);
        }
        catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            return Challenge();
        }
        catch (ApiException ex)
        {
            return StatusCode(ex.StatusCode, ex.Message);
        }
    }

    [HttpGet("/Viajes/{viajeId:int}/Oportunidades/{envioId:int}")]
    public async Task<IActionResult> Detalle(
        int viajeId,
        int envioId,
        CancellationToken cancellationToken)
    {
        try
        {
            return View(await oportunidadesApiClient.ObtenerDetalleAsync(viajeId, envioId, cancellationToken));
        }
        catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            return Challenge();
        }
        catch (ApiException ex)
        {
            return StatusCode(ex.StatusCode, ex.Message);
        }
    }
}
