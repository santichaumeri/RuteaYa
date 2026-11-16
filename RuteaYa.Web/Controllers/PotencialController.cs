using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Web.Models.Optimizador;
using RuteaYa.Web.Models.Potencial;
using RuteaYa.Web.Servicios;

namespace RuteaYa.Web.Controllers;

/// <summary>Pantalla 14: potencial de ganancias mensual (HU-21). El cálculo lo hace la API (HU-20).</summary>
[Authorize]
public sealed class PotencialController(IPotencialApiClient potencialApiClient) : Controller
{
    /// <summary>
    /// Estima el mes elegido (por defecto, el actual) con el criterio elegido.
    /// El formulario es GET, así que cambiar el mes o el criterio vuelve a calcular y la URL se puede compartir.
    /// </summary>
    /// <param name="periodo">Mes en formato yyyy-MM (lo que envía un input type="month").</param>
    /// <param name="criterio">MaximizarGanancias, MinimizarDesvios o Equilibrio.</param>
    [HttpGet]
    public async Task<IActionResult> Index(string? periodo, string? criterio, CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var modelo = new PotencialPaginaViewModel
        {
            Anio = hoy.Year,
            Mes = hoy.Month,
            Criterio = CriteriosOptimizacion.Normalizar(criterio),
            PeriodoMinimo = hoy.ToString("yyyy-MM", CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(periodo))
        {
            if (DateOnly.TryParseExact(periodo + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var elegido))
            {
                modelo.Anio = elegido.Year;
                modelo.Mes = elegido.Month;
            }
            else
            {
                modelo.Error = "El mes elegido no es válido. Te mostramos el mes actual.";
            }
        }

        try
        {
            modelo.Datos = await potencialApiClient.CalcularAsync(modelo.Anio, modelo.Mes, modelo.Criterio, cancellationToken);
        }
        catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            return Challenge();
        }
        catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status400BadRequest)
        {
            // Por ejemplo, un mes que ya terminó: se explica en la misma pantalla y se puede elegir otro.
            modelo.Error = ex.Message;
        }
        catch (ApiException ex)
        {
            return StatusCode(ex.StatusCode, ex.Message);
        }

        return View(modelo);
    }
}
