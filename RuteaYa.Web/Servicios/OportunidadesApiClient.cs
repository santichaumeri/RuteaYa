using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using RuteaYa.Web.Models.Oportunidades;

namespace RuteaYa.Web.Servicios;

public interface IOportunidadesApiClient
{
    Task<IReadOnlyList<OportunidadViewModel>> BuscarAsync(
        int viajeId,
        string orden,
        CancellationToken cancellationToken);

    Task<OportunidadDetalleViewModel> ObtenerDetalleAsync(
        int viajeId,
        int envioId,
        CancellationToken cancellationToken);
}

/// <summary>Cliente de los endpoints api/viajes/{viajeId}/oportunidades (HU-11 y HU-12).</summary>
public sealed class OportunidadesApiClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor) : IOportunidadesApiClient
{
    public async Task<IReadOnlyList<OportunidadViewModel>> BuscarAsync(
        int viajeId,
        string orden,
        CancellationToken cancellationToken)
    {
        using var request = CrearRequestAutenticada(
            $"api/viajes/{viajeId}/oportunidades?orden={Uri.EscapeDataString(orden)}");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await response.EnsureApiSuccessAsync();

        return await response.Content.ReadFromJsonAsync<List<OportunidadViewModel>>(cancellationToken) ?? [];
    }

    public async Task<OportunidadDetalleViewModel> ObtenerDetalleAsync(
        int viajeId,
        int envioId,
        CancellationToken cancellationToken)
    {
        using var request = CrearRequestAutenticada($"api/viajes/{viajeId}/oportunidades/{envioId}");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await response.EnsureApiSuccessAsync();

        return await response.Content.ReadFromJsonAsync<OportunidadDetalleViewModel>(cancellationToken)
            ?? throw new ApiException((int)response.StatusCode, "La API devolvió una respuesta vacía.");
    }

    private HttpRequestMessage CrearRequestAutenticada(string ruta)
    {
        var token = httpContextAccessor.HttpContext?.User.FindFirstValue("access_token");

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "La sesión no contiene un token válido.");
        }

        var request = new HttpRequestMessage(HttpMethod.Get, ruta);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}
