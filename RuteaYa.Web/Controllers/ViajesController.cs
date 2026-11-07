using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Web.Models;
using RuteaYa.Web.Servicios;

namespace RuteaYa.Web.Controllers;

[Authorize]
public sealed class ViajesController(IViajesApiClient viajesApiClient) : Controller
{
    [HttpGet]
    public IActionResult Crear()
    {
        return View(new CrearViajeViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        CrearViajeViewModel modelo,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        try
        {
            await viajesApiClient.CrearViajeAsync(modelo, cancellationToken);
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }

        TempData["MensajeViaje"] = "Viaje creado correctamente.";
        return RedirectToAction("Index", "Dashboard");
    }
}
