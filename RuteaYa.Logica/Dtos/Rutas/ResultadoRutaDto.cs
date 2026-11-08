using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.Logica.Dtos.Rutas;

public class ResultadoRutaDto
{
    public double DistanciaKm {  get; set; }
    public TimeSpan DuracionEstimada { get; set; }
    public List<string> PuntosRecorridos { get; set; } = new();

}
