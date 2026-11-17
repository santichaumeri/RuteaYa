using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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

        // La cookie es de un usuario que ya no existe: se cierra la sesión en vez de mostrar un error.
        if (resultado.UsuarioInexistente)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Mensaje"] = "Tu sesión ya no es válida. Iniciá sesión de nuevo.";
            return RedirectToAction("Login", "Account");
        }

        if (resultado.TieneError || resultado.Resumen is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        return View(resultado.Resumen);
    }
}
