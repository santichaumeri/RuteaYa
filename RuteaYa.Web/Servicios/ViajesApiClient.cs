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
