using RuteaYa.Entidades.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.Logica.Servicios;

public interface IEstadoEnvioService
{
    /// <summary>
    /// Determina si una transición entre dos estados es legal según las reglas del negocio.
    /// </summary>
    bool EstransaccionValida(EstadoEnvio estadoActual, EstadoEnvio estadoNuevo);

    /// <summary>
    /// Valida la transición. Si es inválida, arroja TransicionEstadoInvalidaException.
    /// </summary>
    void ValidarTransicion(EstadoEnvio estadoActual, EstadoEnvio estadoNuevo);

}

