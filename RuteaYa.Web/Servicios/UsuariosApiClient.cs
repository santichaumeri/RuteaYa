using System.Net.Http.Json;
using RuteaYa.Web.Models;

namespace RuteaYa.Web.Servicios;

/// <summary>
/// Contrato del cliente HTTP que habla con UsuariosController de la API.
/// El AccountController depende de esta interfaz, no de HttpClient.
/// </summary>
public interface IUsuariosApiClient
{
    /// <summary>POST api/usuarios/registro. Lanza ApiException si la API responde con error.</summary>
    Task<UsuarioViewModel> RegistrarAsync(RegistroViewModel modelo, CancellationToken cancellationToken);

    /// <summary>POST api/usuarios/login. Lanza ApiException (401) si las credenciales son inválidas.</summary>
    Task<UsuarioViewModel> LoginAsync(LoginViewModel modelo, CancellationToken cancellationToken);
}

/// <summary>
/// Implementación con HttpClient tipado. La BaseAddress (ApiBaseUrl de appsettings) se configura en Program.cs.
/// </summary>
public class UsuariosApiClient : IUsuariosApiClient
{
    private readonly HttpClient _http;

    public UsuariosApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<UsuarioViewModel> RegistrarAsync(RegistroViewModel modelo, CancellationToken cancellationToken)
    {
        // El ViewModel se serializa a JSON con los mismos nombres que espera RegistroRequest.
        var response = await _http.PostAsJsonAsync("api/usuarios/registro", modelo, cancellationToken);
        await response.EnsureApiSuccessAsync();   // si hubo error, lanza ApiException con el mensaje de la API
        return await LeerUsuarioAsync(response, cancellationToken);
    }

    public async Task<UsuarioViewModel> LoginAsync(LoginViewModel modelo, CancellationToken cancellationToken)
    {
        var response = await _http.PostAsJsonAsync("api/usuarios/login", modelo, cancellationToken);
        await response.EnsureApiSuccessAsync();
        return await LeerUsuarioAsync(response, cancellationToken);
    }

    private static async Task<UsuarioViewModel> LeerUsuarioAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var usuario = await response.Content.ReadFromJsonAsync<UsuarioViewModel>(cancellationToken);
        return usuario ?? throw new ApiException((int)response.StatusCode, "La API devolvió una respuesta vacía.");
    }
}
