namespace RuteaYa.Logica.Dtos.Asignaciones;

/// <summary>Pedido de reservar de una vez todos los envíos de una combinación (HU-17).</summary>
public class ConfirmarCombinacionRequestDto
{
    public int ViajeId { get; set; }

    /// <summary>Día de la combinación. La capacidad del viaje es diaria, así que todos los envíos comparten fecha.</summary>
    public DateOnly Fecha { get; set; }

    public List<int> EnvioIds { get; set; } = [];
}
