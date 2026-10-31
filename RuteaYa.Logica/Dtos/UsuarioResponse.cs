namespace RuteaYa.Logica.Dtos;

/// <summary>
/// Lo que la API devuelve de un usuario. Nunca se expone la entidad (ni el PasswordHash).
/// </summary>
public class UsuarioResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
