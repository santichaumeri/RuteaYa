namespace RuteaYa.Entidades;

public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public ICollection<Viaje> Viajes { get; set; } = new List<Viaje>();
    public ICollection<Envio> Envios { get; set; } = new List<Envio>();
}
