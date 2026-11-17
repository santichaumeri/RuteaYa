using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Web.Servicios;

namespace RuteaYa.Web.Controllers;

/// <summary>Pantalla 12 (HU-18): el conductor marca sus paquetes como retirado, en camino y entregado.</summary>
[Authorize]
public class TransportesController(ITransportesApiClient transportesApiClient) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            return View(await transportesApiClient.ObtenerMisTransportesAsync(cancellationToken));
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

    [HttpGet]
    public async Task<IActionResult> Detalle(int id, CancellationToken cancellationToken)
    {
        try
        {
            return View(await transportesApiClient.ObtenerDetalleAsync(id, cancellationToken));
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Retirar(int id, CancellationToken cancellationToken) =>
        CambiarEstadoAsync(() => transportesApiClient.MarcarRetiradoAsync(id, cancellationToken), "retirado");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> EnCamino(int id, CancellationToken cancellationToken) =>
        CambiarEstadoAsync(() => transportesApiClient.MarcarEnCaminoAsync(id, cancellationToken), "en camino");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Entregar(int id, CancellationToken cancellationToken) =>
        CambiarEstadoAsync(() => transportesApiClient.MarcarEntregadoAsync(id, cancellationToken), "entregado");

    private async Task<IActionResult> CambiarEstadoAsync(Func<Task> accion, string estado)
    {
        try
        {
            await accion();
            TempData["Exito"] = $"El paquete fue marcado como {estado}.";
        }
        catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            return Challenge();
        }
        catch (ApiException ex) when (ex.StatusCode is StatusCodes.Status403Forbidden
                                                  or StatusCodes.Status404NotFound
                                                  or StatusCodes.Status409Conflict)
        {
            TempData["Error"] = string.IsNullOrWhiteSpace(ex.Message)
                ? "No se pudo actualizar el estado del paquete."
                : ex.Message;
        }
        catch (ApiException ex)
        {
            return StatusCode(ex.StatusCode, ex.Message);
        }

        return RedirectToAction(nameof(Index));
    }
}
