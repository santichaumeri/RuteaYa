using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using RuteaYa.Web.Models.Optimizador;

namespace RuteaYa.Web.Servicios;

public interface IOptimizadorApiClient
{
    Task<OptimizacionViajeViewModel> CalcularAsync(
        int viajeId,
        DateOnly? fecha,
        string criterio,
        CancellationToken cancellationToken);
}

/// <summary>Cliente de GET api/viajes/{viajeId}/optimizacion (HU-14).</summary>
public sealed class OptimizadorApiClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor) : IOptimizadorApiClient
{
    public async Task<OptimizacionViajeViewModel> CalcularAsync(
        int viajeId,
        DateOnly? fecha,
        string criterio,
        CancellationToken cancellationToken)
    {
        var token = httpContextAccessor.HttpContext?.User.FindFirstValue("access_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "La sesión no contiene un token válido.");
        }

        var ruta = $"api/viajes/{viajeId}/optimizacion?criterio={Uri.EscapeDataString(criterio)}";
        if (fecha is { } dia)
        {
            ruta += "&fecha=" + dia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, ruta);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await response.EnsureApiSuccessAsync();

        return await response.Content.ReadFromJsonAsync<OptimizacionViajeViewModel>(cancellationToken)
            ?? throw new ApiException((int)response.StatusCode, "La API devolvió una respuesta vacía.");
    }
}
