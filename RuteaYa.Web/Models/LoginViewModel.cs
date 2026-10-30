using System.ComponentModel.DataAnnotations;

namespace RuteaYa.Web.Models;

/// <summary>
/// Datos del formulario de login. Las anotaciones activan la validación de cliente (jQuery Validation).
/// Los nombres de las propiedades coinciden con LoginRequest de la API, así se serializa tal cual.
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;
}
