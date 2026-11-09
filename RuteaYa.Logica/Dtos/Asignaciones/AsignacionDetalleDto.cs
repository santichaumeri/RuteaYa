using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.Logica.Dtos.Asignaciones;

public class AsignacionDetalleDto
{
    public int Id { get; set; }
    public int ViajeId { get; set; }
    public int EnvioId { get; set; }
    public DateTime FechaAsignacion { get; set; }
    public decimal DesvioKm { get; set; }
    public decimal Ganancia { get; set; }
    public string EstadoEnvio { get; set; } = string.Empty;
}
