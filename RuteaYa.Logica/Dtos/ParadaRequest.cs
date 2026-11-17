using System.ComponentModel.DataAnnotations;

namespace RuteaYa.Logica.Dtos;

/// <summary>Alta o modificación de una parada intermedia dentro de un viaje.</summary>
public sealed class ParadaRequest
{
    [Required(ErrorMessage = "La dirección de la parada es obligatoria.")]
    [StringLength(300, ErrorMessage = "La dirección de la parada no puede superar los 300 caracteres.")]
    public string Direccion { get; set; } = string.Empty;

    /// <summary>
    /// Posición en el recorrido, empezando en 1. Si no se indica, una parada nueva va al final
    /// y una existente queda donde estaba. Las demás se corren para mantener el orden.
    /// </summary>
    public int? Orden { get; set; }
}
