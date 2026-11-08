using RuteaYa.Logica.Dtos.Oportunidades;

namespace RuteaYa.Logica.Oportunidades;

/// <summary>Envíos compatibles de una fecha y la capacidad que le queda al viaje ese día.</summary>
public sealed record CandidatosFecha(
    DateOnly Fecha,
    CapacidadDisponible Capacidad,
    IReadOnlyList<OportunidadResponse> Oportunidades);

/// <summary>Envíos compatibles con un viaje, agrupados por fecha (de la más próxima a la más lejana).</summary>
public sealed record CandidatosViaje(
    int ViajeId,
    string Origen,
    string Destino,
    CapacidadDisponible CapacidadCompleta,
    IReadOnlyList<CandidatosFecha> Fechas,
    IReadOnlyDictionary<DateOnly, CapacidadDisponible> Ocupacion)
{
    /// <summary>Lo que le queda al viaje en una fecha, aunque esa fecha no tenga oportunidades.</summary>
    public CapacidadDisponible CapacidadEn(DateOnly fecha) =>
        Ocupacion.GetValueOrDefault(fecha, CapacidadCompleta);
}
