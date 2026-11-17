using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Web.Models.Historial;
using RuteaYa.Web.Servicios;

namespace RuteaYa.Web.Controllers;

[Authorize]
public sealed class HistorialController(IHistorialApiClient historialApiClient) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        HistorialFiltroViewModel filtro,
        CancellationToken cancellationToken)
    {
        try
        {
            var modelo = await historialApiClient.ObtenerAsync(filtro, cancellationToken);
            ViewData["Filtro"] = filtro;
            return View(modelo);
        }
        catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            return Challenge();
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(new HistorialResumenViewModel());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id, CancellationToken cancellationToken)
    {
        try
        {
            return View(await historialApiClient.ObtenerDetalleAsync(id, cancellationToken));
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
