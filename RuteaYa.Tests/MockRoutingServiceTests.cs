using RuteaYa.Logica.Servicios.Rutas;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace RuteaYa.Tests;

public class MockRoutingServiceTests
{
    private readonly MockRoutingService _routingService;

    public MockRoutingServiceTests()
    {
        _routingService = new MockRoutingService();
    }

    [Fact]
    public async Task CalcularRutaAsync_MismoOrigenYDestino_RetornaDistanciaCero()
    {
        
        var localidad = "Moron";

        
        var resultado = await _routingService.CalcularRutaAsync(localidad, localidad);

        
        Assert.Equal(0.0, resultado.DistanciaKm);
        Assert.Equal(TimeSpan.FromMinutes(2), resultado.DuracionEstimada);
        Assert.Equal(2, resultado.PuntosRecorridos.Count);
    }

    [Fact]
    public async Task CalcularRutaAsync_LocalidadesConocidas_RetornaDistanciaYDuracionEsperada()
    {
        
        var origen = "Moron";
        var destino = "Castelar"; // Distancia configurada: 2.5 km

        
        var resultado = await _routingService.CalcularRutaAsync(origen, destino);

        
        Assert.Equal(2.5, resultado.DistanciaKm);
        Assert.Equal(TimeSpan.FromMinutes(4), resultado.DuracionEstimada); // 2.5 * 1.5 = 3.75 -> redondeo a 4
        Assert.Equal(new List<string> { "Moron", "Castelar" }, resultado.PuntosRecorridos);
    }

    [Fact]
    public async Task CalcularRutaConParadasAsync_SumaTramosEnOrdenCorrecto()
    {
        // Arrange: Moron -> Haedo (3.0 km) -> Ramos Mejia (2.8 km) = 5.8 km
        var origen = "Moron";
        var paradas = new List<string> { "Haedo" };
        var destino = "Ramos Mejia";

        
        var resultado = await _routingService.CalcularRutaConParadasAsync(origen, paradas, destino);

        
        Assert.Equal(5.8, resultado.DistanciaKm);
        Assert.Equal(3, resultado.PuntosRecorridos.Count);
        Assert.Equal("Moron", resultado.PuntosRecorridos[0]);
        Assert.Equal("Haedo", resultado.PuntosRecorridos[1]);
        Assert.Equal("Ramos Mejia", resultado.PuntosRecorridos[2]);
    }

    [Fact]
    public async Task CalcularDesvioAsync_InsertaOrigenYDestinoDelPaqueteOptimizandoDistancia()
    {
        
        // Viaje: Castelar -> Moron (2.5 km)
        // Paquete: Castelar -> Haedo
        // Candidato óptimo: Castelar -> Moron -> Haedo (2.5 km + 3.0 km = 5.5 km)
        // Desvío esperado: 5.5 - 2.5 = 3.0 km
        var origenViaje = "Castelar";
        var paradasViaje = new List<string>();
        var destinoViaje = "Moron";
        var origenPaquete = "Castelar";
        var destinoPaquete = "Haedo";

        
        var resultado = await _routingService.CalcularDesvioAsync(
            origenViaje, paradasViaje, destinoViaje, origenPaquete, destinoPaquete);

        
        Assert.Equal(2.5, resultado.DistanciaHabitualKm);
        Assert.True(resultado.DistanciaConEnvioKm >= resultado.DistanciaHabitualKm);
        Assert.Equal(3.0, resultado.DesvioKm);
        Assert.True(resultado.TiempoAdicional > TimeSpan.Zero);

        // Verificar que el origen del paquete se visite antes o en el mismo punto que el destino
        var indiceOrigenPaquete = resultado.RecorridoFinal.IndexOf(origenPaquete);
        var indiceDestinoPaquete = resultado.RecorridoFinal.LastIndexOf(destinoPaquete);
        Assert.True(indiceOrigenPaquete < indiceDestinoPaquete);
    }

    [Fact]
    public async Task FallbackDeterministico_MismasCiudadesDesconocidas_DevuelveSiempreMismoResultado()
    {
        
        var ciudadA = "Lujan";
        var ciudadB = "Mercedes";

        
        var res1 = await _routingService.CalcularRutaAsync(ciudadA, ciudadB);
        var res2 = await _routingService.CalcularRutaAsync(ciudadB, ciudadA); // Inverso

        // Assert
        Assert.True(res1.DistanciaKm >= 3.0 && res1.DistanciaKm <= 25.0);
        Assert.Equal(res1.DistanciaKm, res2.DistanciaKm); // Métrica bidireccional consistente
    }
}
