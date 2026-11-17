using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using RuteaYa.Web.Models;
using RuteaYa.Web.Models.Historial;

namespace RuteaYa.Web.Servicios;

public interface IHistorialApiClient
{
    Task<HistorialResumenViewModel> ObtenerAsync(
        HistorialFiltroViewModel filtro,
        CancellationToken cancellationToken);

    Task<HistorialDetalleViewModel> ObtenerDetalleAsync(
        int viajeId,
        CancellationToken cancellationToken);
}

public sealed class HistorialApiClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor) : IHistorialApiClient
{
    public async Task<HistorialResumenViewModel> ObtenerAsync(
        HistorialFiltroViewModel filtro,
        CancellationToken cancellationToken)
    {
        var parametros = new List<string>
        {
            $"pagina={filtro.Pagina}",
            $"pageSize={filtro.PageSize}"
        };

        if (filtro.Desde is DateOnly desde)
        {
            parametros.Add($"desde={desde:yyyy-MM-dd}");
        }

        if (filtro.Hasta is DateOnly hasta)
        {
            parametros.Add($"hasta={hasta:yyyy-MM-dd}");
        }

        if (filtro.Estado is EstadoEnvio estado)
        {
            parametros.Add($"estado={(int)estado}");
        }

        using var request = await CrearRequestAsync(
            HttpMethod.Get,
            $"api/historial?{string.Join('&', parametros)}");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await response.EnsureApiSuccessAsync();

        return await response.Content.ReadFromJsonAsync<HistorialResumenViewModel>(cancellationToken)
            ?? throw new ApiException((int)response.StatusCode, "La API devolvió una respuesta vacía.");
    }

    public async Task<HistorialDetalleViewModel> ObtenerDetalleAsync(
        int viajeId,
        CancellationToken cancellationToken)
    {
        using var request = await CrearRequestAsync(HttpMethod.Get, $"api/historial/{viajeId}");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await response.EnsureApiSuccessAsync();

        return await response.Content.ReadFromJsonAsync<HistorialDetalleViewModel>(cancellationToken)
            ?? throw new ApiException((int)response.StatusCode, "La API devolvió una respuesta vacía.");
    }

    private async Task<HttpRequestMessage> CrearRequestAsync(HttpMethod method, string uri)
    {
        var token = httpContextAccessor.HttpContext?.User.FindFirstValue("access_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "La sesión no contiene un token válido.");
        }

        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await Task.FromResult(request);
    }
}
