using System.ComponentModel.DataAnnotations;

namespace RuteaYa.Web.Models;

/// <summary>
/// Regla de horario del alta y la edición de viajes, en el formulario (antes de llamar a la API).
/// La API valida lo mismo en ViajeService; acá solo se adelanta el mensaje al lado del campo.
/// </summary>
internal static class ValidacionHorarioViaje
{
    public const string Mensaje = "La hora de llegada tiene que ser posterior a la hora de salida.";

    public static IEnumerable<ValidationResult> Validar(TimeSpan horaSalida, TimeSpan horaLlegada)
    {
        if (horaLlegada <= horaSalida)
        {
            yield return new ValidationResult(Mensaje, [nameof(CrearViajeViewModel.HoraLlegada)]);
        }
    }
}
