using RuteaYa.Logica.Dtos.Rutas;
using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.Logica.Servicios.Rutas;

public class MockRoutingService : IRoutingService
{
    // Velocidad promedio simulada: 40 km/h (1.5 minutos por km)
    private const double MinutosPorKm = 1.5;

    private static readonly Dictionary<string, Dictionary<string, double>> DistanciasConocidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Moron"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Haedo"] = 3.0,
            ["Castelar"] = 2.5,
            ["Ituzaingo"] = 5.0,
            ["Ramos Mejia"] = 5.5,
            ["San Justo"] = 7.0,
            ["CABA"] = 22.0
        },
        ["Castelar"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Moron"] = 2.5,
            ["Ituzaingo"] = 3.0,
            ["Haedo"] = 5.0,
            ["Padua"] = 6.0,
            ["Merlo"] = 9.0
        },
        ["Ituzaingo"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Castelar"] = 3.0,
            ["Moron"] = 5.0,
            ["Padua"] = 3.5,
            ["Merlo"] = 6.5,
            ["Haedo"] = 7.5
        },
        ["Haedo"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Moron"] = 3.0,
            ["Ramos Mejia"] = 2.8,
            ["Castelar"] = 5.0,
            ["San Justo"] = 4.5,
            ["CABA"] = 19.0
        },
        ["Ramos Mejia"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Haedo"] = 2.8,
            ["Moron"] = 5.5,
            ["San Justo"] = 3.2,
            ["Ciudadela"] = 2.0,
            ["CABA"] = 16.0
        }
    };

    public Task<ResultadoRutaDto> CalcularRutaAsync(string origen, string destino)
    {
        var puntos = new List<string> { origen, destino };
        var distancia = ObtenerDistanciaTramo(origen, destino);
        var duracion = CalcularDuracion(distancia);

        return Task.FromResult(new ResultadoRutaDto
        {
            DistanciaKm = Math.Round(distancia, 2),
            DuracionEstimada = duracion,
            PuntosRecorridos = puntos
        });
    }

    

    public Task<ResultadoRutaDto> CalcularRutaConParadasAsync(string origen, List<string> paradas, string destino)
    {
        var rutaCompleta = new List<string> { origen };
        
        if (paradas != null && paradas.Count > 0)
        {
            rutaCompleta.AddRange(paradas);
        }
        rutaCompleta.Add(destino);

        double distanciaTotal = 0;

        for (int i = 0; i < rutaCompleta.Count - 1; i++)
        {
            distanciaTotal += ObtenerDistanciaTramo(rutaCompleta[i], rutaCompleta[i + 1]);
        }

        return Task.FromResult(new ResultadoRutaDto
        {
            DistanciaKm = Math.Round(distanciaTotal, 2),
            DuracionEstimada = CalcularDuracion(distanciaTotal),
            PuntosRecorridos = rutaCompleta
        });
    }

    public async Task<CalculoDesvioDto> CalcularDesvioAsync(string origenViaje, List<string> paradasViaje, string destinoViaje, string origenPaquete, string destinoPaquete)
    {
        // 1. Ruta base del viaje habitual
        var rutaHabitual = await CalcularRutaConParadasAsync(origenViaje, paradasViaje, destinoViaje);

        // Puntos base: [Origen, p1, p2, ..., Destino]
        var puntosBase = new List<string>(rutaHabitual.PuntosRecorridos);

        // 2. Probar todas las posiciones válidas de inserción de origen y destino del paquete.
        // Reglas del recorrido: el viaje empieza en su origen y termina en su destino (ninguno se mueve),
        // y el paquete se retira antes de entregarse.
        double menorDistanciaTotal = double.MaxValue;
        List<string> mejorRecorrido = new();

        int n = puntosBase.Count;

        // i: posición del origen del paquete, entre el origen del viaje y su destino (índices 1..n-1).
        for (int i = 1; i < n; i++)
        {
            // j: posición del destino del paquete, después del retiro y antes del destino del viaje,
            // que tras la primera inserción quedó en el índice n.
            for (int j = i + 1; j <= n; j++)
            {
                var candidato = new List<string>(puntosBase);
                candidato.Insert(i, origenPaquete);
                candidato.Insert(j, destinoPaquete);

                double distCandidato = 0;
                for (int k = 0; k < candidato.Count - 1; k++)
                {
                    distCandidato += ObtenerDistanciaTramo(candidato[k], candidato[k + 1]);
                }

                if (distCandidato < menorDistanciaTotal)
                {
                    menorDistanciaTotal = distCandidato;
                    mejorRecorrido = candidato;
                }
            }
        }
        
        var duracionConEnvio = CalcularDuracion(menorDistanciaTotal);

        return new CalculoDesvioDto
        {
            DistanciaHabitualKm = rutaHabitual.DistanciaKm,
            DuracionHabitual = rutaHabitual.DuracionEstimada,
            DistanciaConEnvioKm = Math.Round(menorDistanciaTotal, 2),
            DuracionConEnvio = duracionConEnvio,
            RecorridoFinal = mejorRecorrido
        };
    }

    private double ObtenerDistanciaTramo(string o, string d)
    {
        if(String.Equals(o.Trim(), d.Trim(),StringComparison.OrdinalIgnoreCase))
        {
            return 0.0;
        }

        if (DistanciasConocidas.TryGetValue(o, out var destinosA) && destinosA.TryGetValue(d, out var d1))
        {
            return d1;
        }

        if (DistanciasConocidas.TryGetValue(d, out var destinosB) && destinosB.TryGetValue(o, out var d2))
        {
            return d2;
        }

        
        return GenerarDistanciaDeterministica(o, d);

    }

    private double GenerarDistanciaDeterministica(string o, string d)
    {
        var combinada = string.Compare(o, d, StringComparison.OrdinalIgnoreCase) < 0
            ? $"{o.ToLowerInvariant()}-{d.ToLowerInvariant()}"
            : $"{d.ToLowerInvariant()}-{o.ToLowerInvariant()}";

        
        // string.GetHashCode() cambia en cada ejecución del proceso; FNV-1a da siempre el mismo valor.
        uint hash = 2166136261;
        foreach (var caracter in combinada)
        {
            hash = (hash ^ caracter) * 16777619;
        }
        double km = 3.0 + (hash % 220) / 10.0;
        return Math.Round(km, 2);
    }

    private static TimeSpan CalcularDuracion(double distanciaKm)
    {
        var minutos = distanciaKm * MinutosPorKm;

        return TimeSpan.FromMinutes(Math.Max(2, Math.Round(minutos)));
    }
}
