using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Web.Models;
using RuteaYa.Web.Servicios;

namespace RuteaYa.Web.Controllers;

/// <summary>
/// Registro, login y logout. La API valida las credenciales; acá se crea (o borra) la cookie de sesión.
/// </summary>
public class AccountController : Controller
{
    private readonly IUsuariosApiClient _usuariosApi;

    public AccountController(IUsuariosApiClient usuariosApi)
    {
        _usuariosApi = usuariosApi;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // Si ya hay sesión no tiene sentido mostrar el login.
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel modelo, string? returnUrl, CancellationToken cancellationToken)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        try
        {
            // El cliente HTTP llama a la API, que verifica el hash y devuelve los datos del usuario.
            var usuario = await _usuariosApi.LoginAsync(modelo, cancellationToken);
            await IniciarSesionAsync(usuario);
        }
        catch (ApiException ex)
        {
            // Mensaje de la API (ej. "Credenciales inválidas."); se vacía la contraseña antes de volver a mostrar.
            ModelState.AddModelError(string.Empty, ex.Message);
            modelo.Password = string.Empty;
            ModelState.Remove(nameof(LoginViewModel.Password));
            return View(modelo);
        }

        // Url.IsLocalUrl evita redirigir a sitios externos (open redirect).
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Registro()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegistroViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(RegistroViewModel modelo, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        try
        {
            await _usuariosApi.RegistrarAsync(modelo, cancellationToken);
        }
        catch (ApiException ex)
        {
            // Por ejemplo 409: "Ya existe un usuario registrado con ese email."
            ModelState.AddModelError(string.Empty, ex.Message);
            modelo.Password = string.Empty;
            modelo.ConfirmarPassword = string.Empty;
            ModelState.Remove(nameof(RegistroViewModel.Password));
            ModelState.Remove(nameof(RegistroViewModel.ConfirmarPassword));
            return View(modelo);
        }

        TempData["Mensaje"] = "Cuenta creada. Ya podés iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    // Convierte los datos del usuario en claims y los guarda en la cookie de autenticación.
    private async Task IniciarSesionAsync(UsuarioViewModel usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, $"{usuario.Nombre} {usuario.Apellido}"),
            new(ClaimTypes.Email, usuario.Email)
        };
        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad));
    }
}
