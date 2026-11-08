using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.Logica.Dtos.Rutas;

public class CalculoDesvioDto
{
    public double DistanciaHabitualKm { get; set; }
    public TimeSpan DuracionHabitual { get; set; }

    public double DistanciaConEnvioKm { get; set; }
    public TimeSpan DuracionConEnvio { get; set; }

    public double DesvioKm => Math.Max(0, Math.Round(DistanciaConEnvioKm - DistanciaHabitualKm, 2));
    public TimeSpan TiempoAdicional => DuracionConEnvio > DuracionHabitual
        ? DuracionConEnvio - DuracionHabitual
        : TimeSpan.Zero;

    public List<string> RecorridoFinal { get; set; } = new();
}
