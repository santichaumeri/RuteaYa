using System.Net.Http.Headers;
using System.Security.Claims;
using RuteaYa.Web.Models;
using RuteaYa.Web.Models.Envios;

namespace RuteaYa.Web.Servicios;

public class EnviosApiClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor) : IEnviosApiClient
{
    public async Task<IReadOnlyList<EnvioViewModel>> ObtenerMisEnviosAsync(EstadoEnvio? estado, CancellationToken cancellationToken)
    {
        var ruta = estado.HasValue ? $"api/envios?estado={(int)estado.Value}" : "api/envios";
        using var request = CrearRequestAutenticada(HttpMethod.Get, ruta);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await response.EnsureApiSuccessAsync();

        return await response.Content.ReadFromJsonAsync<List<EnvioViewModel>>(cancellationToken) ?? [];
    }

    public async Task<EnvioViewModel> ObtenerPorIdAsync(int id, CancellationToken cancellationToken)
    {
        using var request = CrearRequestAutenticada(HttpMethod.Get, $"api/envios/{id}");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await response.EnsureApiSuccessAsync();

        return await LeerEnvioAsync(response, cancellationToken);
    }

    public async Task<EnvioViewModel> CrearEnvioAsync(CrearEnvioViewModel modelo, CancellationToken cancellationToken)
    {
        using var request = CrearRequestAutenticada(HttpMethod.Post, "api/envios");
        request.Content = JsonContent.Create(modelo);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await response.EnsureApiSuccessAsync();

        return await LeerEnvioAsync(response, cancellationToken);
    }

    public async Task CancelarEnvioAsync(int id, CancellationToken cancellationToken)
    {
        using var request = CrearRequestAutenticada(HttpMethod.Post, $"api/envios/{id}/cancelar");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await response.EnsureApiSuccessAsync();
    }

    /// <summary>Arma el pedido con el token JWT guardado en la cookie de sesión.</summary>
    private HttpRequestMessage CrearRequestAutenticada(HttpMethod metodo, string ruta)
    {
        var token = httpContextAccessor.HttpContext?.User.FindFirstValue("access_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "La sesión no contiene un token válido.");
        }

        var request = new HttpRequestMessage(metodo, ruta);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static async Task<EnvioViewModel> LeerEnvioAsync(HttpResponseMessage response, CancellationToken cancellationToken) =>
        await response.Content.ReadFromJsonAsync<EnvioViewModel>(cancellationToken)
            ?? throw new ApiException((int)response.StatusCode, "La API devolvió una respuesta vacía.");
}
