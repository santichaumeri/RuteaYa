namespace RuteaYa.Logica.Optimizacion;

/// <summary>
/// Un envío que ya es compatible con el viaje (fecha, día y orden del recorrido los filtra HU-11)
/// y que el optimizador puede combinar con otros.
/// </summary>
/// <param name="EnvioId">Id del envío.</param>
/// <param name="Pago">Monto ofrecido por el remitente.</param>
/// <param name="PesoKg">Peso del paquete.</param>
/// <param name="DesvioKm">Kilómetros extra que agrega llevar este envío al recorrido habitual.</param>
public sealed record CandidatoEnvio(int EnvioId, decimal Pago, decimal PesoKg, decimal DesvioKm);
