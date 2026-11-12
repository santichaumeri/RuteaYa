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

    /// <summary>
    /// Pantalla 11 (HU-17): resumen previo de la combinación. No escribe nada, así que Volver no modifica datos.
    /// </summary>
    [HttpGet("/Viajes/{viajeId:int}/Optimizador/Confirmar")]
    public async Task<IActionResult> Confirmar(
        int viajeId,
        DateOnly fecha,
        string? criterio,
        [FromQuery] List<int> envioIds,
        CancellationToken cancellationToken)
    {
        var modelo = NuevoResumen(viajeId, fecha, criterio);

        if (envioIds.Count == 0)
        {
            modelo.Error = "No hay envíos para confirmar. Volvé al optimizador y elegí una combinación.";
            return View(modelo);
        }

        try
        {
            await CompletarResumenAsync(modelo, envioIds, cancellationToken);
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

    /// <summary>
    /// Confirmar y reservar (HU-17): todo o nada. Si algún envío ya no está disponible, la API responde 409
    /// con cuáles son y se vuelve a mostrar el resumen para recalcular o cancelar.
    /// </summary>
    [HttpPost("/Viajes/{viajeId:int}/Optimizador/Confirmar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarReserva(
        int viajeId,
        DateOnly fecha,
        string? criterio,
        [FromForm] List<int> envioIds,
        CancellationToken cancellationToken)
    {
        try
        {
            var reservada = await optimizadorApiClient.ConfirmarCombinacionAsync(viajeId, fecha, envioIds, cancellationToken);

            TempData["Exito"] = $"¡Combinación reservada! {reservada.CantidadPaquetes} " +
                $"{(reservada.CantidadPaquetes == 1 ? "envío quedó asignado" : "envíos quedaron asignados")} " +
                $"a tu viaje por {reservada.GananciaTotal:C0}.";

            return RedirectToAction(nameof(Index), new
            {
                viajeId,
                fecha = fecha.ToString("yyyy-MM-dd"),
                criterio = CriteriosOptimizacion.Normalizar(criterio)
            });
        }
        catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            return Challenge();
        }
        catch (ApiException ex) when (ex.StatusCode is StatusCodes.Status400BadRequest or StatusCodes.Status409Conflict)
        {
            // No se reservó ninguno. Se muestra el motivo y, si la API lo informó, cuáles envíos se perdieron.
            var modelo = NuevoResumen(viajeId, fecha, criterio);
            modelo.Error = ex.Message;
            modelo.EnviosNoDisponibles.AddRange(ex.EnviosNoDisponibles);

            try
            {
                await CompletarResumenAsync(modelo, envioIds, cancellationToken);
            }
            catch (ApiException otro) when (otro.StatusCode == StatusCodes.Status401Unauthorized)
            {
                return Challenge();
            }
            catch (ApiException)
            {
                // Sin el detalle de los envíos igual se muestra el motivo original del rechazo.
            }

            return View(nameof(Confirmar), modelo);
        }
        catch (ApiException ex)
        {
            return StatusCode(ex.StatusCode, ex.Message);
        }
    }

    private static ConfirmarCombinacionViewModel NuevoResumen(int viajeId, DateOnly fecha, string? criterio) => new()
    {
        ViajeId = viajeId,
        Fecha = fecha,
        Criterio = CriteriosOptimizacion.Normalizar(criterio)
    };

    /// <summary>
    /// Completa el resumen con los datos actuales de la API. Los envíos pedidos que ya no figuran entre los
    /// candidatos del viaje en esa fecha se marcan como no disponibles.
    /// </summary>
    private async Task CompletarResumenAsync(
        ConfirmarCombinacionViewModel modelo,
        IReadOnlyList<int> envioIds,
        CancellationToken cancellationToken)
    {
        var datos = await optimizadorApiClient.CalcularAsync(modelo.ViajeId, modelo.Fecha, modelo.Criterio, cancellationToken);

        modelo.ViajeOrigen = datos.ViajeOrigen;
        modelo.ViajeDestino = datos.ViajeDestino;

        foreach (var id in envioIds.Distinct())
        {
            if (modelo.EnviosNoDisponibles.Contains(id))
            {
                continue;
            }

            if (datos.Candidato(id) is { } candidato)
            {
                modelo.Envios.Add(candidato);
            }
            else
            {
                modelo.EnviosNoDisponibles.Add(id);
            }
        }

        if (modelo.EnviosNoDisponibles.Count > 0)
        {
            modelo.Error ??= "Algunos envíos de la combinación ya no están disponibles.";
        }
    }
}
