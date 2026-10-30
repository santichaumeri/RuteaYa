using System.ComponentModel.DataAnnotations;

namespace RuteaYa.Logica.Dtos;

/// <summary>Credenciales que llegan a la API para iniciar sesión.</summary>
public class LoginRequest
{
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = string.Empty;
}
