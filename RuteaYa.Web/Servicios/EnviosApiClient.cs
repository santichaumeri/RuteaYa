using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Dtos.Envios;
using System.Net.Http.Headers;

namespace RuteaYa.Web.Servicios;

public class EnviosApiClient : IEnviosApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EnviosApiClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    private void AdjuntarTokenAutenticacion()
    {
        var token = _httpContextAccessor.HttpContext?.User.FindFirst("access_token")?.Value;

        if(!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<List<EnvioDetalleDto>> ObtenerMisEnviosAsync(EstadoEnvio? estado = null)
    {
        AdjuntarTokenAutenticacion();

        var url = "api/envios";
        if (estado.HasValue)
        {
            url += $"?estado={estado.Value}";
        }

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            return new List<EnvioDetalleDto>();
        }

        return await response.Content.ReadFromJsonAsync<List<EnvioDetalleDto>>()
               ?? new List<EnvioDetalleDto>();
    }

    public async Task<EnvioDetalleDto?> ObtenerPorIdAsync(int id)
    {
        AdjuntarTokenAutenticacion();

        var response = await _httpClient.GetAsync($"api/envios/{id}");
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<EnvioDetalleDto>();
    }

    public async Task<EnvioDetalleDto?> CrearEnvioAsync(CrearEnvioDto dto)
    {
        AdjuntarTokenAutenticacion();

        var response = await _httpClient.PostAsJsonAsync("api/envios", dto);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<EnvioDetalleDto>();
    }

    public async Task<bool> CancelarEnvioAsync(int id)
    {
        AdjuntarTokenAutenticacion();

        var response = await _httpClient.PostAsync($"api/envios/{id}/cancelar", null);
        return response.IsSuccessStatusCode;
    }
}
