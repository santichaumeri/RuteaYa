using RuteaYa.Entidades.Enums;

namespace RuteaYa.Logica.Dtos.Transportes;

/// <summary>Un paquete que el conductor aceptó llevar (HU-18), con las acciones que hoy puede hacer.</summary>
public class TransporteDto
{
    public int AsignacionId { get; set; }
    public int ViajeId { get; set; }
    public string ViajeOrigen { get; set; } = string.Empty;
    public string ViajeDestino { get; set; } = string.Empty;

    public int EnvioId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal PesoKg { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;

    public EstadoEnvio Estado { get; set; }
    public decimal Ganancia { get; set; }

    // Acciones válidas según el estado actual (reglas de HU-09).
    public bool PuedeMarcarRetirado { get; set; }
    public bool PuedeMarcarEnCamino { get; set; }
    public bool PuedeMarcarEntregado { get; set; }
}
