using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Dtos.Envios;
using RuteaYa.Web.Servicios;

namespace RuteaYa.Web.Controllers;

[Authorize]
public class EnviosController : Controller
{
    private readonly IEnviosApiClient _enviosApiClient;

    public EnviosController(IEnviosApiClient enviosApiClient)
    {
        _enviosApiClient = enviosApiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(EstadoEnvio? estado = null)
    {
        ViewBag.EstadoSeleccionado = estado;
        var envios = await _enviosApiClient.ObtenerMisEnviosAsync(estado);
        return View(envios);
    }

    [HttpGet]
    public IActionResult Crear()
    {
        var modelo = new CrearEnvioDto
        {
            Fecha = DateOnly.FromDateTime(DateTime.Today)
        };
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearEnvioDto dto)
    {
        if(!ModelState.IsValid)
        {
            return View(dto);
        }

        var resultado = await _enviosApiClient.CrearEnvioAsync(dto);

        if(resultado == null)
        {
            ModelState.AddModelError(string.Empty, "Ocurrió un error al intentar publicar el envío. Verifique los datos o intente nuevamente.");
            return View(dto);
        }

        TempData["MensajeExito"] = "¡El envío fue publicado exitosamente!";
        return RedirectToAction("Index", "Envios");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        var exito = await _enviosApiClient.CancelarEnvioAsync(id);
        if (exito)
        {
            TempData["MensajeExito"] = "El envío ha sido cancelado.";
        }
        else
        {
            TempData["MensajeError"] = "No se pudo cancelar el envío. Verifique su estado actual.";
        }

        return RedirectToAction(nameof(Index));
    }


}
