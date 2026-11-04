using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Web.Models.Dashboard;
using RuteaYa.Web.Servicios;

namespace RuteaYa.Web.Controllers;

[Authorize]
public sealed class DashboardController(DashboardApiClient dashboardApiClient) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var resultado = await dashboardApiClient.ObtenerResumenAsync(cancellationToken);

        if (resultado.EstaNoAutorizado)
        {
            return Challenge();
        }

        if (resultado.TieneError || resultado.Resumen is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        return View(resultado.Resumen);
    }
}
