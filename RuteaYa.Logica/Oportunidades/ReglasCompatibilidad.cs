using System.Globalization;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;

namespace RuteaYa.Logica.Oportunidades;

/// <summary>Qué tan conveniente es un envío según cuánto del desvío máximo del viaje consume.</summary>
public enum NivelCompatibilidad
{
    MuyCompatible = 1,
    Compatible = 2,
    MuchoDesvio = 3
}

/// <summary>Lo que todavía le entra a un viaje en una fecha, descontando los envíos ya asignados.</summary>
public sealed record CapacidadDisponible(int Paquetes, decimal PesoKg, decimal DesvioKm);

/// <summary>
/// Reglas de compatibilidad entre un viaje y un envío. Son la única fuente de verdad:
/// las usan la búsqueda (HU-11), el detalle (HU-12) y la verificación antes de aceptar.
/// </summary>
public static class ReglasCompatibilidad
{
    /// <summary>Hasta este porcentaje del desvío máximo se considera "Muy compatible".</summary>
    public const decimal UmbralMuyCompatible = 0.4m;

    /// <summary>Hasta este porcentaje del desvío máximo se considera "Compatible"; arriba, "Mucho desvío".</summary>
    public const decimal UmbralCompatible = 0.8m;

    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

    public static NivelCompatibilidad Clasificar(decimal desvioKm, decimal desvioMaximoKm)
    {
        if (desvioMaximoKm <= 0)
        {
            return desvioKm <= 0 ? NivelCompatibilidad.MuyCompatible : NivelCompatibilidad.MuchoDesvio;
        }

        var proporcion = desvioKm / desvioMaximoKm;

        return proporcion <= UmbralMuyCompatible ? NivelCompatibilidad.MuyCompatible
            : proporcion <= UmbralCompatible ? NivelCompatibilidad.Compatible
            : NivelCompatibilidad.MuchoDesvio;
    }

    public static string Etiqueta(NivelCompatibilidad nivel) => nivel switch
    {
        NivelCompatibilidad.MuyCompatible => "Muy compatible",
        NivelCompatibilidad.Compatible => "Compatible",
        _ => "Mucho desvío"
    };

    /// <summary>Pago dividido por los km adicionales. Es null cuando el envío no agrega desvío (no hay división por cero).</summary>
    public static decimal? GananciaPorKmAdicional(decimal pago, decimal desvioKm) =>
        desvioKm <= 0 ? null : Math.Round(pago / desvioKm, 2);

    /// <summary>Si el envío se hace en un día en que el viaje sale y todavía no pasó.</summary>
    public static bool CoincideFecha(Viaje viaje, DateOnly fechaEnvio, DateOnly hoy) =>
        fechaEnvio >= hoy && viaje.DiasSemana.HasFlag(ADiaSemana(fechaEnvio.DayOfWeek));

    /// <summary>
    /// Motivos por los que el envío no puede sumarse al viaje que no dependen de la ruta.
    /// Sirven para descartar sin consultar el servicio de rutas.
    /// </summary>
    public static List<string> MotivosSinRuta(
        Viaje viaje,
        Envio envio,
        int conductorId,
        CapacidadDisponible capacidad,
        DateOnly hoy)
    {
        var motivos = new List<string>();

        if (viaje.Estado != EstadoViaje.Activo)
        {
            motivos.Add("El viaje está inactivo.");
        }

        if (envio.UsuarioId == conductorId)
        {
            motivos.Add("No podés llevar un envío publicado por vos.");
        }

        if (envio.Estado != EstadoEnvio.BuscandoConductor)
        {
            motivos.Add("El envío ya no está disponible: ya fue asignado, cancelado o finalizado.");
        }

        if (envio.Fecha < hoy)
        {
            motivos.Add(string.Format(Cultura, "La fecha del envío ({0:dd/MM/yyyy}) ya pasó.", envio.Fecha));
        }
        else if (!viaje.DiasSemana.HasFlag(ADiaSemana(envio.Fecha.DayOfWeek)))
        {
            motivos.Add(string.Format(Cultura, "El viaje no sale el {0:dddd dd/MM/yyyy}.", envio.Fecha));
        }

        if (capacidad.Paquetes < 1)
        {
            motivos.Add("El viaje ya completó la cantidad máxima de paquetes para ese día.");
        }

        if (envio.PesoKg > capacidad.PesoKg)
        {
            motivos.Add(string.Format(
                Cultura,
                "Supera el peso disponible: {0:0.##} kg y quedan {1:0.##} kg.",
                envio.PesoKg,
                Math.Max(0, capacidad.PesoKg)));
        }

        return motivos;
    }

    /// <summary>Motivo de descarte por desvío, o null si el desvío entra en lo que queda disponible.</summary>
    public static string? MotivoDesvio(decimal desvioKm, CapacidadDisponible capacidad) =>
        desvioKm <= capacidad.DesvioKm
            ? null
            : string.Format(
                Cultura,
                "Supera el desvío disponible: {0:0.##} km y quedan {1:0.##} km.",
                desvioKm,
                Math.Max(0, capacidad.DesvioKm));

    private static DiasSemana ADiaSemana(DayOfWeek dia) => dia switch
    {
        DayOfWeek.Monday => DiasSemana.Lunes,
        DayOfWeek.Tuesday => DiasSemana.Martes,
        DayOfWeek.Wednesday => DiasSemana.Miercoles,
        DayOfWeek.Thursday => DiasSemana.Jueves,
        DayOfWeek.Friday => DiasSemana.Viernes,
        DayOfWeek.Saturday => DiasSemana.Sabado,
        _ => DiasSemana.Domingo
    };
}
