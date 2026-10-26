using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using Xunit;

namespace RuteaYa.Tests;

public class EntidadesTests
{
    [Fact]
    public void Envio_nuevo_arranca_buscando_conductor()
    {
        var envio = new Envio();

        Assert.Equal(EstadoEnvio.BuscandoConductor, envio.Estado);
    }

    [Fact]
    public void Viaje_nuevo_arranca_activo()
    {
        var viaje = new Viaje();

        Assert.Equal(EstadoViaje.Activo, viaje.Estado);
    }
}
