namespace RuteaYa.Logica.Dtos.Envios;

/// <summary>
/// Viaje que lleva el envío, visto por quien lo publicó (pantalla 7: "Consultar viaje asociado").
/// El viaje es del conductor, así que solo se exponen los datos que necesita el remitente.
/// </summary>
public class ViajeAsociadoDto
{
    public int ViajeId { get; set; }

    /// <summary>Nombre y apellido del conductor.</summary>
    public string Conductor { get; set; } = string.Empty;

    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public TimeSpan HoraSalida { get; set; }
    public TimeSpan HoraLlegada { get; set; }

    /// <summary>Paradas intermedias del viaje, en orden.</summary>
    public List<string> Paradas { get; set; } = [];

    public DateTime FechaAsignacion { get; set; }
}
