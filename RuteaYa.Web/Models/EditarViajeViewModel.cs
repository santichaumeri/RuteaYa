using System.ComponentModel.DataAnnotations;

namespace RuteaYa.Web.Models;

public sealed class EditarViajeViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El origen es obligatorio.")]
    [StringLength(300, ErrorMessage = "El origen no puede superar los 300 caracteres.")]
    [Display(Name = "Origen")]
    public string Origen { get; set; } = string.Empty;

    [Required(ErrorMessage = "El destino es obligatorio.")]
    [StringLength(300, ErrorMessage = "El destino no puede superar los 300 caracteres.")]
    [Display(Name = "Destino")]
    public string Destino { get; set; } = string.Empty;

    [MinLength(1, ErrorMessage = "Seleccioná al menos un día de la semana.")]
    public List<int> DiasSeleccionados { get; set; } = [];

    [DataType(DataType.Time)]
    [Display(Name = "Hora de salida")]
    public TimeSpan HoraSalida { get; set; }

    [DataType(DataType.Time)]
    [Display(Name = "Hora de llegada")]
    public TimeSpan HoraLlegada { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad máxima de paquetes debe ser mayor a cero.")]
    [Display(Name = "Máximo de paquetes")]
    public int MaxPaquetes { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "El peso máximo debe ser mayor a cero.")]
    [Display(Name = "Peso máximo (kg)")]
    public decimal PesoMaximoKg { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "El desvío máximo debe ser mayor a cero.")]
    [Display(Name = "Desvío máximo (km)")]
    public decimal DesvioMaximoKm { get; set; }

    public List<string> Paradas { get; set; } = [];
}
