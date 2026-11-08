using RuteaYa.Logica.Dtos.Rutas;
using RuteaYa.Logica.Servicios.Rutas;

namespace RuteaYa.Tests;

/// <summary>Ruta habitual de 10 km (15 min); el desvío depende del destino del paquete. 1.5 min por km extra.</summary>
internal sealed class RutasFalsas : IRoutingService
{
    private static readonly Dictionary<string, double> DesvioPorDestino = new()
    {
        ["En el camino"] = 0,
        ["Medio"] = 0.5,
        ["Al lado"] = 1,
        ["A"] = 2,
        ["Cerca"] = 3,
        ["Lejos"] = 4,
        ["Borde"] = 4.5,
        ["MuyLejos"] = 6
    };

    public Task<ResultadoRutaDto> CalcularRutaAsync(string origen, string destino) =>
        throw new NotSupportedException();

    public Task<ResultadoRutaDto> CalcularRutaConParadasAsync(string origen, List<string> paradas, string destino) =>
        throw new NotSupportedException();

    public Task<CalculoDesvioDto> CalcularDesvioAsync(
        string origenViaje,
        List<string> paradasViaje,
        string destinoViaje,
        string origenPaquete,
        string destinoPaquete)
    {
        var desvio = DesvioPorDestino[destinoPaquete];

        return Task.FromResult(new CalculoDesvioDto
        {
            DistanciaHabitualKm = 10,
            DuracionHabitual = TimeSpan.FromMinutes(15),
            DistanciaConEnvioKm = 10 + desvio,
            DuracionConEnvio = TimeSpan.FromMinutes(15 + desvio * 1.5),
            RecorridoFinal = [origenViaje, origenPaquete, destinoPaquete, destinoViaje]
        });
    }
}

internal sealed class RelojFijo(DateOnly hoy) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(hoy.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
