using System.Net;
using System.Net.Http.Json;
using RuteaYa.Web.Models.Dashboard;

namespace RuteaYa.Web.Servicios;

public sealed class DashboardApiClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
{
    public async Task<DashboardApiResult> ObtenerResumenAsync(
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/dashboard/resumen");
        var token = httpContextAccessor.HttpContext?.User.FindFirst("access_token")?.Value;
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return DashboardApiResult.NoAutorizadoResult();
        }

        if (!response.IsSuccessStatusCode)
        {
            return DashboardApiResult.ErrorResult();
        }

        var resumen = await response.Content.ReadFromJsonAsync<DashboardViewModel>(cancellationToken);
        return resumen is null
            ? DashboardApiResult.ErrorResult()
            : DashboardApiResult.ExitosoResult(resumen);
    }
}

public sealed record DashboardApiResult(
    DashboardViewModel? Resumen,
    bool EstaNoAutorizado,
    bool TieneError)
{
    public static DashboardApiResult ExitosoResult(DashboardViewModel resumen) => new(resumen, false, false);
    public static DashboardApiResult NoAutorizadoResult() => new(null, true, false);
    public static DashboardApiResult ErrorResult() => new(null, false, true);
}
