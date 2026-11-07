using RuteaYa.Entidades.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace RuteaYa.Logica.Dtos.Envios;

public class CrearEnvioDto : IValidatableObject
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
        if(!string.IsNullOrWhiteSpace(Origen) &&
           !string.IsNullOrWhiteSpace(Destino) &&
           Origen.Trim().Equals(Destino.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "El origen y el destino no pueden ser iguales.",
                new[] { nameof(Destino) });  
        }

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        if (Fecha < hoy)
        {
            yield return new ValidationResult(
                "La fecha del envío no puede ser anterior a hoy.",
                new[] { nameof(Fecha) });
        }
    }
}
