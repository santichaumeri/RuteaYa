using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Servicios;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace RuteaYa.Tests;

public class EstadoEnvioServiceTest
{
    private readonly EstadoEnvioService _servicio = new();

    [Theory]
    // Transiciones válidas (camino feliz)
    [InlineData(EstadoEnvio.BuscandoConductor, EstadoEnvio.ConductorAsignado, true)]
    [InlineData(EstadoEnvio.BuscandoConductor, EstadoEnvio.Cancelado, true)]
    [InlineData(EstadoEnvio.ConductorAsignado, EstadoEnvio.Retirado, true)]
    [InlineData(EstadoEnvio.ConductorAsignado, EstadoEnvio.Cancelado, true)]
    [InlineData(EstadoEnvio.Retirado, EstadoEnvio.EnCamino, true)]
    [InlineData(EstadoEnvio.EnCamino, EstadoEnvio.Retirado, true)]

    // Violaciones de reglas del enunciado
    [InlineData(EstadoEnvio.BuscandoConductor, EstadoEnvio.Entregado, false)]
    [InlineData(EstadoEnvio.Entregado, EstadoEnvio.BuscandoConductor, false)]
    [InlineData(EstadoEnvio.Entregado, EstadoEnvio.Cancelado, false)]
    [InlineData(EstadoEnvio.Cancelado, EstadoEnvio.ConductorAsignado, false)]
    [InlineData(EstadoEnvio.Retirado, EstadoEnvio.Cancelado, false)]
    [InlineData(EstadoEnvio.EnCamino, EstadoEnvio.Cancelado, false)]

    public void EsTransicionValida_DebeResponderSegunMatriz(EstadoEnvio actual, EstadoEnvio nuevo, bool esperado)
    {
        var resultado = _servicio.EstransaccionValida(actual, nuevo);
        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public void ValidarTransicion_SiEsInvalida_DebeLanzarTransicionEstadoInvalidaException()
    {
        var ex = Assert.Throws<TransicionEstadoInvalidaException>(() => _servicio.ValidarTransicion(EstadoEnvio.Cancelado, EstadoEnvio.ConductorAsignado));

        Assert.Equal(EstadoEnvio.Cancelado, ex.EstadoActual);
        Assert.Equal(EstadoEnvio.ConductorAsignado, ex.EstadoNuevo);

    }

    [Fact]
    public void ValidarTransicion_SiEsValida_NoDebeLanzarExcepcion()
    {
        var exception = Record.Exception(() =>
            _servicio.ValidarTransicion(EstadoEnvio.BuscandoConductor, EstadoEnvio.ConductorAsignado));

        Assert.Null(exception);
    }
}




