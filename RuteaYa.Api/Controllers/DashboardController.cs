using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Logica.Dtos.Dashboard;
using RuteaYa.Logica.Servicios;

namespace RuteaYa.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("resumen")]
    [ProducesResponseType(typeof(DashboardResumenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerResumen(CancellationToken cancellationToken)
    {
        var claimUsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(claimUsuarioId, out var usuarioId))
        {
            return Unauthorized();
        }

        var resumen = await dashboardService.ObtenerResumenAsync(usuarioId, cancellationToken);
        return resumen is null ? NotFound() : Ok(resumen);
    }
}
