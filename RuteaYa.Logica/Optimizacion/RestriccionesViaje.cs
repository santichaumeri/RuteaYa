namespace RuteaYa.Logica.Optimizacion;

/// <summary>Límites del viaje que ninguna combinación puede superar.</summary>
public sealed record RestriccionesViaje(int MaxPaquetes, decimal PesoMaximoKg, decimal DesvioMaximoKm);
