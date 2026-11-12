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

    /// <summary>
    /// Reserva de una vez todos los envíos de una combinación (HU-17). Si la API responde 409 porque algún
    /// envío ya no está disponible, lanza una ApiException con EnviosNoDisponibles completa.
    /// </summary>
    Task<CombinacionConfirmadaViewModel> ConfirmarCombinacionAsync(
        int viajeId,
        DateOnly fecha,
        IReadOnlyList<int> envioIds,
        CancellationToken cancellationToken);
}

/// <summary>Cliente de api/viajes/{viajeId}/optimizacion (HU-14) y api/asignaciones/combinacion (HU-17).</summary>
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

    public async Task<CombinacionConfirmadaViewModel> ConfirmarCombinacionAsync(
        int viajeId,
        DateOnly fecha,
        IReadOnlyList<int> envioIds,
        CancellationToken cancellationToken)
    {
        var token = httpContextAccessor.HttpContext?.User.FindFirstValue("access_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "La sesión no contiene un token válido.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/asignaciones/combinacion")
        {
            Content = JsonContent.Create(new { viajeId, fecha, envioIds })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await response.EnsureApiSuccessAsync();

        return await response.Content.ReadFromJsonAsync<CombinacionConfirmadaViewModel>(cancellationToken)
            ?? throw new ApiException((int)response.StatusCode, "La API devolvió una respuesta vacía.");
    }
}
