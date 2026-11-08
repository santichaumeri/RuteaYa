using RuteaYa.Web.Models.Dashboard;

namespace RuteaYa.Web.Models;

/// <summary>Selector de viaje compartido por las pantallas Oportunidades y Optimizador.</summary>
/// <param name="MisViajes">Viajes activos del usuario.</param>
/// <param name="ViajeIdSeleccionado">Viaje elegido, o null si todavía no se eligió.</param>
/// <param name="TextoViajeSeleccionado">Texto a mostrar si el viaje elegido no está en la lista.</param>
/// <param name="Controlador">Controller al que lleva cada opción (acción Index con viajeId).</param>
public sealed record SelectorViajeViewModel(
    IReadOnlyList<ViajeResumenViewModel> MisViajes,
    int? ViajeIdSeleccionado,
    string? TextoViajeSeleccionado,
    string Controlador);
