using System.ComponentModel.DataAnnotations;

namespace RuteaYa.Web.Models.Envios;

/// <summary>Formulario "Nuevo envío" (pantalla 6). La API vuelve a validar todo al recibirlo.</summary>
public class CrearEnvioViewModel : IValidatableObject
{
    [Required(ErrorMessage = "El origen es obligatorio.")]
    [StringLength(150, ErrorMessage = "El origen no puede superar los 150 caracteres.")]
    public string Origen { get; set; } = string.Empty;

    [Required(ErrorMessage = "El destino es obligatorio.")]
    [StringLength(150, ErrorMessage = "El destino no puede superar los 150 caracteres.")]
    public string Destino { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha es obligatoria.")]
    public DateOnly Fecha { get; set; }

    [Range(0.01, 10000.0, ErrorMessage = "El peso debe ser mayor a 0 kg.")]
    public decimal PesoKg { get; set; }

    [Required(ErrorMessage = "El tamaño del paquete es obligatorio.")]
    public TamanoPaquete Tamano { get; set; }

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    [Range(0.01, 10000000.0, ErrorMessage = "El monto ofrecido debe ser mayor a 0.")]
    public decimal MontoOfrecido { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(Origen) &&
            !string.IsNullOrWhiteSpace(Destino) &&
            Origen.Trim().Equals(Destino.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "El origen y el destino no pueden ser iguales.",
                [nameof(Destino)]);
        }

        if (Fecha < DateOnly.FromDateTime(DateTime.Today))
        {
            yield return new ValidationResult(
                "La fecha del envío no puede ser anterior a hoy.",
                [nameof(Fecha)]);
        }
    }
}

/// <summary>Envío propio, para Mis envíos y el detalle (pantalla 7).</summary>
public class EnvioViewModel
{
    public int Id { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public decimal PesoKg { get; set; }
    public TamanoPaquete Tamano { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal MontoOfrecido { get; set; }
    public EstadoEnvio Estado { get; set; }

    /// <summary>Null mientras el envío no tiene conductor asignado.</summary>
    public ViajeAsociadoViewModel? ViajeAsociado { get; set; }
}

/// <summary>Viaje que lleva el envío, visto por quien lo publicó.</summary>
public class ViajeAsociadoViewModel
{
    public int ViajeId { get; set; }
    public string Conductor { get; set; } = string.Empty;
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public TimeSpan HoraSalida { get; set; }
    public TimeSpan HoraLlegada { get; set; }
    public List<string> Paradas { get; set; } = [];
    public DateTime FechaAsignacion { get; set; }
}
