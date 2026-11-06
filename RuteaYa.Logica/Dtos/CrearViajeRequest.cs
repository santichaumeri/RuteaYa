using System.ComponentModel.DataAnnotations;
using RuteaYa.Entidades.Enums;

namespace RuteaYa.Logica.Dtos;

public sealed class CrearViajeRequest
{
    [Required(ErrorMessage = "El origen es obligatorio.")]
    [StringLength(300, ErrorMessage = "El origen no puede superar los 300 caracteres.")]
    public string Origen { get; set; } = string.Empty;

    [Required(ErrorMessage = "El destino es obligatorio.")]
    [StringLength(300, ErrorMessage = "El destino no puede superar los 300 caracteres.")]
    public string Destino { get; set; } = string.Empty;

    public DiasSemana DiasSemana { get; set; }

    public TimeSpan HoraSalida { get; set; }

    public TimeSpan HoraLlegada { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad máxima de paquetes debe ser mayor a cero.")]
    public int MaxPaquetes { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "El peso máximo debe ser mayor a cero.")]
    public decimal PesoMaximoKg { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "El desvío máximo debe ser mayor a cero.")]
    public decimal DesvioMaximoKm { get; set; }

    public List<CrearParadaRequest> Paradas { get; set; } = [];
}
