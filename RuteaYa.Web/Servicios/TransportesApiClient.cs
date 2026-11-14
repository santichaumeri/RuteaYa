using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using RuteaYa.Logica.Dtos.Transportes;

namespace RuteaYa.Web.Servicios;

public interface ITransportesApiClient
{
    Task<IReadOnlyList<TransporteDto>> ObtenerMisTransportesAsync(CancellationToken cancellationToken);
    Task<TransporteDto> ObtenerDetalleAsync(int asignacionId, CancellationToken cancellationToken);
    Task MarcarRetiradoAsync(int asignacionId, CancellationToken cancellationToken);
    Task MarcarEnCaminoAsync(int asignacionId, CancellationToken cancellationToken);
    Task MarcarEntregadoAsync(int asignacionId, CancellationToken cancellationToken);
}

/// <summary>Cliente de los endpoints api/transportes (HU-18).</summary>
public sealed class TransportesApiClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor) : ITransportesApiClient
{
    public async Task<IReadOnlyList<TransporteDto>> ObtenerMisTransportesAsync(CancellationToken cancellationToken)
    {
        using var request = CrearRequestAutenticada("api/transportes", HttpMethod.Get);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await response.EnsureApiSuccessAsync();

        return await response.Content.ReadFromJsonAsync<List<TransporteDto>>(cancellationToken)
               ?? [];
    }

    public async Task<TransporteDto> ObtenerDetalleAsync(int asignacionId, CancellationToken cancellationToken)
    {
        using var request = CrearRequestAutenticada($"api/transportes/{asignacionId}", HttpMethod.Get);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await response.EnsureApiSuccessAsync();

        return (await response.Content.ReadFromJsonAsync<TransporteDto>(cancellationToken))!;
    }

    public Task MarcarRetiradoAsync(int asignacionId, CancellationToken cancellationToken) =>
        PostAsync($"api/transportes/{asignacionId}/retirar", cancellationToken);

    public Task MarcarEnCaminoAsync(int asignacionId, CancellationToken cancellationToken) =>
        PostAsync($"api/transportes/{asignacionId}/en-camino", cancellationToken);

    public Task MarcarEntregadoAsync(int asignacionId, CancellationToken cancellationToken) =>
        PostAsync($"api/transportes/{asignacionId}/entregar", cancellationToken);

    private async Task PostAsync(string ruta, CancellationToken cancellationToken)
    {
        using var request = CrearRequestAutenticada(ruta, HttpMethod.Post);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await response.EnsureApiSuccessAsync();
    }

    private HttpRequestMessage CrearRequestAutenticada(string ruta, HttpMethod metodo)
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
}
