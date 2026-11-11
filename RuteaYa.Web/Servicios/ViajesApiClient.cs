using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using RuteaYa.Web.Models;

namespace RuteaYa.Web.Servicios;

public interface IViajesApiClient
{
    Task<ViajeResponseViewModel> CrearViajeAsync(
        CrearViajeViewModel modelo,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ViajeResponseViewModel>> ObtenerMisViajesAsync(
        int? estado,
        CancellationToken cancellationToken);

    Task DesactivarViajeAsync(int viajeId, CancellationToken cancellationToken);

    Task<ViajeDetalleViewModel> ObtenerDetalleAsync(
        int viajeId,
        CancellationToken cancellationToken);

    Task<ViajeDetalleViewModel> EditarViajeAsync(
        int viajeId,
        EditarViajeViewModel modelo,
        CancellationToken cancellationToken);
}

public sealed class ViajesApiClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor) : IViajesApiClient
{
    public async Task<ViajeResponseViewModel> CrearViajeAsync(
        CrearViajeViewModel modelo,
        CancellationToken cancellationToken)
    {
        var token = httpContextAccessor.HttpContext?
            .User.FindFirstValue("access_token");

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ApiException(
                StatusCodes.Status401Unauthorized,
                "La sesión no contiene un token válido.");
        }

        var diasSemana = modelo.DiasSeleccionados
            .Aggregate(0, (actual, dia) => actual | dia);

        var request = new
        {
            modelo.Origen,
            modelo.Destino,
            DiasSemana = diasSemana,
            modelo.HoraSalida,
            modelo.HoraLlegada,
            modelo.MaxPaquetes,
            modelo.PesoMaximoKg,
            modelo.DesvioMaximoKm,
            Paradas = modelo.Paradas
                .Select(direccion => new { Direccion = direccion })
                .ToList()
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "api/viajes")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(
            httpRequest,
            cancellationToken);

        await response.EnsureApiSuccessAsync();

        var viaje = await response.Content.ReadFromJsonAsync<ViajeResponseViewModel>(
            cancellationToken);

        return viaje ?? throw new ApiException(
            (int)response.StatusCode,
            "La API devolvió una respuesta vacía.");
    }

    public async Task<ViajeDetalleViewModel> ObtenerDetalleAsync(
        int viajeId,
        CancellationToken cancellationToken)
    {
        using var request = await CrearRequestAutenticadaAsync(
            HttpMethod.Get,
            $"api/viajes/{viajeId}");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await response.EnsureApiSuccessAsync();

        return await LeerDetalleAsync(response, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ViajeResponseViewModel>> ObtenerMisViajesAsync(
        int? estado,
        CancellationToken cancellationToken)
    {
        using var request = await CrearRequestAutenticadaAsync(
            HttpMethod.Get,
            estado is null ? "api/viajes" : $"api/viajes?estado={estado.Value}");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await response.EnsureApiSuccessAsync();

        return await response.Content.ReadFromJsonAsync<List<ViajeResponseViewModel>>(cancellationToken)
            ?? [];
    }

    public async Task DesactivarViajeAsync(int viajeId, CancellationToken cancellationToken)
    {
        using var request = await CrearRequestAutenticadaAsync(
            HttpMethod.Post,
            $"api/viajes/{viajeId}/desactivar");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await response.EnsureApiSuccessAsync();
    }

    public async Task<ViajeDetalleViewModel> EditarViajeAsync(
        int viajeId,
        EditarViajeViewModel modelo,
        CancellationToken cancellationToken)
    {
        var diasSemana = modelo.DiasSeleccionados
            .Aggregate(0, (actual, dia) => actual | dia);

        var requestBody = new
        {
            modelo.Origen,
            modelo.Destino,
            DiasSemana = diasSemana,
            modelo.HoraSalida,
            modelo.HoraLlegada,
            modelo.MaxPaquetes,
            modelo.PesoMaximoKg,
            modelo.DesvioMaximoKm,
            Paradas = modelo.Paradas
                .Select(direccion => new { Direccion = direccion })
                .ToList()
        };

        using var request = await CrearRequestAutenticadaAsync(
            HttpMethod.Put,
            $"api/viajes/{viajeId}");
        request.Content = JsonContent.Create(requestBody);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await response.EnsureApiSuccessAsync();

        return await LeerDetalleAsync(response, cancellationToken);
    }

    private async Task<HttpRequestMessage> CrearRequestAutenticadaAsync(
        HttpMethod metodo,
        string ruta)
    {
        var token = httpContextAccessor.HttpContext?
            .User.FindFirstValue("access_token");

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ApiException(
                StatusCodes.Status401Unauthorized,
                "La sesión no contiene un token válido.");
        }

        var request = new HttpRequestMessage(metodo, ruta);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static async Task<ViajeDetalleViewModel> LeerDetalleAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var detalle = await response.Content.ReadFromJsonAsync<ViajeDetalleViewModel>(
            cancellationToken);

        return detalle ?? throw new ApiException(
            (int)response.StatusCode,
            "La API devolvió una respuesta vacía.");
    }
}

public sealed class ViajeResponseViewModel
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public int DiasSemana { get; set; }
    public TimeSpan HoraSalida { get; set; }
    public TimeSpan HoraLlegada { get; set; }
    public int MaxPaquetes { get; set; }
    public decimal PesoMaximoKg { get; set; }
    public decimal DesvioMaximoKm { get; set; }
    public int Estado { get; set; }
    public IReadOnlyCollection<ParadaResponseViewModel> Paradas { get; set; } = [];
}

public sealed class ParadaResponseViewModel
{
    public int Id { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public int Orden { get; set; }
}
