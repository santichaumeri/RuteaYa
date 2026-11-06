using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Excepciones;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Markup;

namespace RuteaYa.Logica.Servicios;

public class EstadoEnvioService : IEstadoEnvioService
{
    // Mapeo exhaustivo y explícito de transiciones permitidas (O(1) lookup)
    private static readonly IReadOnlyDictionary<EstadoEnvio, HashSet<EstadoEnvio>> TransaccionesValidas = new Dictionary<EstadoEnvio, HashSet<EstadoEnvio>>
    {
        [EstadoEnvio.BuscandoConductor] = new() { EstadoEnvio.ConductorAsignado, EstadoEnvio.Cancelado},
        [EstadoEnvio.ConductorAsignado] = new() { EstadoEnvio.Retirado, EstadoEnvio.Cancelado },
        [EstadoEnvio.Retirado] = new() { EstadoEnvio.EnCamino },
        [EstadoEnvio.EnCamino] = new() { EstadoEnvio.Retirado },
        [EstadoEnvio.Entregado] = new(),
        [EstadoEnvio.Cancelado] = new()

    };

    public bool EstransaccionValida(EstadoEnvio estadoActual, EstadoEnvio estadoNuevo)
    {
        if(TransaccionesValidas.TryGetValue(estadoActual,out var permitidos))
        {
            return permitidos.Contains(estadoNuevo);
        }
        return false;
    }

    public void ValidarTransicion(EstadoEnvio estadoActual, EstadoEnvio estadoNuevo)
    {
        if(!EstransaccionValida(estadoActual, estadoNuevo)) 
        {
            throw new TransicionEstadoInvalidaException(estadoActual, estadoNuevo);
        }
    }
}
