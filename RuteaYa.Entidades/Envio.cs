using RuteaYa.Entidades.Enums;

namespace RuteaYa.Entidades;

public class Envio
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public decimal PesoKg { get; set; }
    public TamanoPaquete Tamano { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal MontoOfrecido { get; set; }
    public EstadoEnvio Estado { get; set; } = EstadoEnvio.BuscandoConductor;

    /// <summary>Token de concurrencia: detecta si otro usuario tomó el envío al mismo tiempo.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Asignacion? Asignacion { get; set; }
}
