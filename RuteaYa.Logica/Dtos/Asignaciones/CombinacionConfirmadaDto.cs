namespace RuteaYa.Logica.Dtos.Asignaciones;

/// <summary>Resultado de reservar una combinación completa (HU-17).</summary>
public class CombinacionConfirmadaDto
{
    public int ViajeId { get; set; }
    public DateOnly Fecha { get; set; }

    public List<AsignacionDetalleDto> Asignaciones { get; set; } = [];

    public int CantidadPaquetes { get; set; }
    public decimal GananciaTotal { get; set; }
    public decimal PesoTotalKg { get; set; }
    public decimal DesvioTotalKm { get; set; }
}
