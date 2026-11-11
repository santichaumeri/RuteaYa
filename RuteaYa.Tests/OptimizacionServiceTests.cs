using RuteaYa.Logica.Dtos.Optimizacion;
using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Optimizacion;
using RuteaYa.Logica.Optimizacion.Criterios;
using RuteaYa.Logica.Servicios;
using Xunit;

namespace RuteaYa.Tests;

public class OptimizacionServiceTests
{
    private readonly OptimizacionService _servicio = new(
        new OptimizadorEnvios(),
        new CatalogoCriterios([CriterioMaximizarGanancias.Instancia, new CriterioMinimizarDesvios(), new CriterioEquilibrio(2500m)]));

    [Fact]
    public void Calcular_devuelve_la_combinacion_del_enunciado_como_DTO()
    {
        var request = new OptimizarRequest
        {
            MaxPaquetes = 3,
            PesoMaximoKg = 10m,
            DesvioMaximoKm = 5m,
            Candidatos =
            [
                new() { EnvioId = 1, Pago = 4000m, PesoKg = 2m, DesvioKm = 1.0m },
                new() { EnvioId = 2, Pago = 5500m, PesoKg = 6m, DesvioKm = 2.0m },
                new() { EnvioId = 3, Pago = 3500m, PesoKg = 4m, DesvioKm = 1.0m },
                new() { EnvioId = 4, Pago = 7000m, PesoKg = 8m, DesvioKm = 3.0m },
                new() { EnvioId = 5, Pago = 2500m, PesoKg = 1m, DesvioKm = 0.5m }
            ]
        };

        var respuesta = _servicio.Calcular(request);

        Assert.NotNull(respuesta.MejorCombinacion);
        Assert.Equal([1, 2, 5], respuesta.MejorCombinacion.EnvioIds);
        Assert.Equal(3, respuesta.MejorCombinacion.CantidadPaquetes);
        Assert.Equal(12000m, respuesta.MejorCombinacion.GananciaTotal);
        Assert.Equal(9m, respuesta.MejorCombinacion.PesoTotalKg);
        Assert.Equal(3.5m, respuesta.MejorCombinacion.DesvioTotalKm);
        Assert.Empty(respuesta.MejorCombinacion.MotivosDescarte);
        Assert.Equal(11000m, respuesta.Alternativas[0].GananciaTotal);
        Assert.All(respuesta.Descartadas, d => Assert.NotEmpty(d.MotivosDescarte));
        Assert.True(respuesta.CombinacionesEvaluadas > 0);
    }

    [Fact]
    public void Calcular_con_request_nulo_lanza_ValidacionException()
    {
        Assert.Throws<ValidacionException>(() => _servicio.Calcular(null!));
    }

    [Theory]
    [InlineData(CriterioOptimizacion.MaximizarGanancias, new[] { 1, 2, 5 })]
    [InlineData(CriterioOptimizacion.MinimizarDesvios, new[] { 5 })]
    [InlineData(CriterioOptimizacion.Equilibrio, new[] { 1, 3, 5 })]
    public void Calcular_aplica_el_criterio_pedido(CriterioOptimizacion criterio, int[] esperado)
    {
        var request = RequestDelEnunciado();
        request.Criterio = criterio;

        var respuesta = _servicio.Calcular(request);

        Assert.Equal(criterio, respuesta.Criterio);
        Assert.False(string.IsNullOrWhiteSpace(respuesta.CriterioDescripcion));
        Assert.Equal(esperado, respuesta.MejorCombinacion!.EnvioIds);
    }

    [Fact]
    public void Calcular_informa_la_ganancia_neta_solo_con_Equilibrio()
    {
        var request = RequestDelEnunciado();
        request.Criterio = CriterioOptimizacion.Equilibrio;

        var equilibrio = _servicio.Calcular(request);
        var porGanancia = _servicio.Calcular(RequestDelEnunciado());

        // A+C+E: $10.000 − $2.500 × 2,5 km
        Assert.Equal(3750m, equilibrio.MejorCombinacion!.GananciaNeta);
        Assert.Null(porGanancia.MejorCombinacion!.GananciaNeta);
    }

    [Fact]
    public void Calcular_con_criterio_inexistente_lanza_ValidacionException()
    {
        var request = RequestDelEnunciado();
        request.Criterio = (CriterioOptimizacion)99;

        Assert.Throws<ValidacionException>(() => _servicio.Calcular(request));
    }

    private static OptimizarRequest RequestDelEnunciado() => new()
    {
        MaxPaquetes = 3,
        PesoMaximoKg = 10m,
        DesvioMaximoKm = 5m,
        Candidatos =
        [
            new() { EnvioId = 1, Pago = 4000m, PesoKg = 2m, DesvioKm = 1.0m },
            new() { EnvioId = 2, Pago = 5500m, PesoKg = 6m, DesvioKm = 2.0m },
            new() { EnvioId = 3, Pago = 3500m, PesoKg = 4m, DesvioKm = 1.0m },
            new() { EnvioId = 4, Pago = 7000m, PesoKg = 8m, DesvioKm = 3.0m },
            new() { EnvioId = 5, Pago = 2500m, PesoKg = 1m, DesvioKm = 0.5m }
        ]
    };
}
