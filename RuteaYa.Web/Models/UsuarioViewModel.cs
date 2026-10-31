namespace RuteaYa.Web.Models;

/// <summary>
/// Usuario tal como lo devuelve la API (equivale a UsuarioResponse, pero propio del MVC:
/// el Web no referencia a la Logica). Con estos datos se arman los claims de la cookie.
/// </summary>
public class UsuarioViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
