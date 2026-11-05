using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Logica.Dtos;
using RuteaYa.Logica.Servicios;

namespace RuteaYa.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/viajes")]
public sealed class ViajesController(IViajeService viajeService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ViajeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ViajeResponse>> Crear(
        CrearViajeRequest request,
        CancellationToken cancellationToken)
    {
        var claimUsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(claimUsuarioId, out var usuarioId))
        {
            return Unauthorized();
        }

        var viaje = await viajeService.CrearViajeAsync(
            usuarioId,
            request,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, viaje);
    }
}
