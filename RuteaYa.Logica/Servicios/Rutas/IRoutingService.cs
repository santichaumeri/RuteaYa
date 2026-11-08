using RuteaYa.Logica.Dtos.Rutas;
using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.Logica.Servicios.Rutas;

public interface IRoutingService
{
    /// <summary>
    /// Calcula la ruta directa entre un origen y un destino.
    /// </summary>
    Task<ResultadoRutaDto> CalcularRutaAsync(string origen, string destino);

    /// <summary>
    /// Calcula la ruta habitual de un viaje considerando paradas intermedias.
    /// </summary>
    Task<ResultadoRutaDto> CalcularRutaConParadasAsync(string origen, List<string> paradas, string destino);

    /// <summary>
    /// Evalúa el impacto de incorporar un paquete (origen y destino) dentro del recorrido de un viaje habitual.
    /// Determina la distancia adicional (desvío) y el tiempo extra en el mejor orden posible.
    /// </summary>
    Task<CalculoDesvioDto> CalcularDesvioAsync(
        string origenViaje,
        List<string> paradasViaje,
        string destinoViaje,
        string origenPaquete,
        string destinoPaquete);
}
