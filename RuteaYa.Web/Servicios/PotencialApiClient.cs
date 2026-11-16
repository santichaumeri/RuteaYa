using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using RuteaYa.Web.Models.Potencial;

namespace RuteaYa.Web.Servicios;

public interface IPotencialApiClient
{
    /// <summary>
    /// Estimación del mes con el criterio elegido (HU-20). Lanza ApiException con el mensaje de la API
    /// si el mes no es válido o ya terminó (400) o si la sesión venció (401).
    /// </summary>
    Task<PotencialMensualViewModel> CalcularAsync(
        int anio,
        int mes,
        string criterio,
        CancellationToken cancellationToken);
}

/// <summary>Cliente de GET api/potencial (pantalla 14, HU-21).</summary>
public sealed class PotencialApiClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor) : IPotencialApiClient
{
    public async Task<PotencialMensualViewModel> CalcularAsync(
        int anio,
        int mes,
        string criterio,
        CancellationToken cancellationToken)
    {
        var token = httpContextAccessor.HttpContext?.User.FindFirstValue("access_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "La sesión no contiene un token válido.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/potencial?anio={anio}&mes={mes}&criterio={Uri.EscapeDataString(criterio)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await response.EnsureApiSuccessAsync();

        return await response.Content.ReadFromJsonAsync<PotencialMensualViewModel>(cancellationToken)
            ?? throw new ApiException((int)response.StatusCode, "La API devolvió una respuesta vacía.");
    }
}
