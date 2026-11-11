using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Web.Models;
using RuteaYa.Web.Servicios;

namespace RuteaYa.Web.Controllers;

[Authorize]
public sealed class ViajesController(IViajesApiClient viajesApiClient) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string estado = "activos", CancellationToken cancellationToken = default)
    {
        var filtro = estado.ToLowerInvariant() switch
        {
            "inactivos" => (int?)2,
            "todos" => null,
            _ => (int?)1
        };

        try
        {
            var viajes = await viajesApiClient.ObtenerMisViajesAsync(filtro, cancellationToken);
            ViewData["Filtro"] = estado.ToLowerInvariant() switch
            {
                "inactivos" => "inactivos",
                "todos" => "todos",
                _ => "activos"
            };
            return View(viajes);
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(Array.Empty<ViajeResponseViewModel>());
        }
    }

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

    [HttpGet]
    public async Task<IActionResult> Detalle(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var detalle = await viajesApiClient.ObtenerDetalleAsync(id, cancellationToken);
            return View(detalle);
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
    public async Task<IActionResult> Editar(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var detalle = await viajesApiClient.ObtenerDetalleAsync(id, cancellationToken);
            return View(ConvertirAEditar(detalle));
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
    public async Task<IActionResult> Editar(
        int id,
        EditarViajeViewModel modelo,
        CancellationToken cancellationToken)
    {
        modelo.Id = id;

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        try
        {
            await viajesApiClient.EditarViajeAsync(id, modelo, cancellationToken);
        }
        catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            return Challenge();
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }

        TempData["MensajeViaje"] = "Viaje actualizado correctamente.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    private static EditarViajeViewModel ConvertirAEditar(ViajeDetalleViewModel detalle)
    {
        var dias = new[] { 1, 2, 4, 8, 16, 32, 64 }
            .Where(dia => (detalle.DiasSemana & dia) == dia)
            .ToList();

        return new EditarViajeViewModel
        {
            Id = detalle.Id,
            Origen = detalle.Origen,
            Destino = detalle.Destino,
            DiasSeleccionados = dias,
            HoraSalida = detalle.HoraSalida,
            HoraLlegada = detalle.HoraLlegada,
            MaxPaquetes = detalle.MaxPaquetes,
            PesoMaximoKg = detalle.PesoMaximoKg,
            DesvioMaximoKm = detalle.DesvioMaximoKm,
            Paradas = detalle.Paradas
                .OrderBy(parada => parada.Orden)
                .Select(parada => parada.Direccion)
                .ToList()
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(int id, CancellationToken cancellationToken)
    {
        try
        {
            await viajesApiClient.DesactivarViajeAsync(id, cancellationToken);
            TempData["MensajeViaje"] = "Viaje desactivado correctamente.";
        }
        catch (ApiException ex)
        {
            TempData["ErrorViaje"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
