using System.ComponentModel.DataAnnotations;

namespace RuteaYa.Logica.Dtos;

public sealed class CrearParadaRequest
{
    [Required(ErrorMessage = "La dirección de la parada es obligatoria.")]
    [StringLength(300, ErrorMessage = "La dirección de la parada no puede superar los 300 caracteres.")]
    public string Direccion { get; set; } = string.Empty;
}
