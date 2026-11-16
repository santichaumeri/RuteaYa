using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Web.Models;
using RuteaYa.Web.Servicios;

namespace RuteaYa.Web.Controllers;

[Authorize]
public sealed class ViajesController(IViajesApiClient viajesApiClient) : Controller
{
    /// <summary>Valor del botón "Calcular ruta": el formulario se envía pero no se guarda.</summary>
    private const string AccionCalcularRuta = "calcular-ruta";

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
        string? accion,
        CancellationToken cancellationToken)
    {
        if (accion == AccionCalcularRuta)
        {
            modelo.Ruta = await CalcularRutaAsync(modelo.Origen, modelo.Destino, modelo.Paradas, cancellationToken);
            return View(modelo);
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        ViajeResponseViewModel creado;
        try
        {
            creado = await viajesApiClient.CrearViajeAsync(modelo, cancellationToken);
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }

        // El detalle muestra la ruta calculada del viaje recién guardado.
        TempData["MensajeViaje"] = "Viaje creado correctamente.";
        return RedirectToAction(nameof(Detalle), new { id = creado.Id });
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
        string? accion,
        CancellationToken cancellationToken)
    {
        modelo.Id = id;

        if (accion == AccionCalcularRuta)
        {
            modelo.Ruta = await CalcularRutaAsync(modelo.Origen, modelo.Destino, modelo.Paradas, cancellationToken);
            return View(modelo);
        }

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

    /// <summary>
    /// Calcula la ruta con lo que haya cargado en el formulario, sin guardar el viaje.
    /// Solo importa el recorrido: los errores de días, horarios o capacidad se muestran al guardar.
    /// </summary>
    private async Task<RutaViajeViewModel?> CalcularRutaAsync(
        string? origen,
        string? destino,
        List<string> paradas,
        CancellationToken cancellationToken)
    {
        ModelState.Clear();

        if (string.IsNullOrWhiteSpace(origen) || string.IsNullOrWhiteSpace(destino))
        {
            ModelState.AddModelError(string.Empty, "Completá el origen y el destino para calcular la ruta.");
            return null;
        }

        try
        {
            return await viajesApiClient.CalcularRutaAsync(
                origen,
                destino,
                paradas.Select(parada => parada ?? string.Empty).ToList(),
                cancellationToken);
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return null;
        }
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
                .ToList(),
            Ruta = detalle.Ruta
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
