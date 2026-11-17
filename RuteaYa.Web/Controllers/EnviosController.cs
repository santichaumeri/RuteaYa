using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Web.Models;
using RuteaYa.Web.Models.Envios;
using RuteaYa.Web.Servicios;

namespace RuteaYa.Web.Controllers;

[Authorize]
public class EnviosController(IEnviosApiClient enviosApiClient) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(EstadoEnvio? estado, CancellationToken cancellationToken)
    {
        ViewBag.EstadoSeleccionado = estado;

        try
        {
            return View(await enviosApiClient.ObtenerMisEnviosAsync(estado, cancellationToken));
        }
        catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            return Challenge();
        }
        catch (ApiException ex)
        {
            TempData["MensajeError"] = ex.Message;
            return View(Array.Empty<EnvioViewModel>());
        }
    }

    /// <summary>Detalle del envío y, si ya tiene conductor, el viaje que lo lleva (pantalla 7).</summary>
    [HttpGet]
    public async Task<IActionResult> Detalle(int id, CancellationToken cancellationToken)
    {
        try
        {
            return View(await enviosApiClient.ObtenerPorIdAsync(id, cancellationToken));
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
    public IActionResult Crear()
    {
        return View(new CrearEnvioViewModel { Fecha = DateOnly.FromDateTime(DateTime.Today) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearEnvioViewModel modelo, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        try
        {
            await enviosApiClient.CrearEnvioAsync(modelo, cancellationToken);
        }
        catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            return Challenge();
        }
        catch (ApiException ex)
        {
            // El motivo que dio la API (por ejemplo, una regla de negocio) se muestra arriba del formulario.
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }

        TempData["MensajeExito"] = "¡El envío fue publicado exitosamente!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id, CancellationToken cancellationToken)
    {
        try
        {
            await enviosApiClient.CancelarEnvioAsync(id, cancellationToken);
            TempData["MensajeExito"] = "El envío ha sido cancelado.";
        }
        catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            return Challenge();
        }
        catch (ApiException ex)
        {
            // Por ejemplo, el 409 de la API cuando el envío ya fue retirado.
            TempData["MensajeError"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
