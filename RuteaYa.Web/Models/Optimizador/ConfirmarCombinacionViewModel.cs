using RuteaYa.Web.Models.Oportunidades;

namespace RuteaYa.Web.Models.Optimizador;

/// <summary>Pantalla 11: resumen previo para confirmar y reservar una combinación (HU-17).</summary>
public sealed class ConfirmarCombinacionViewModel
{
    public int ViajeId { get; set; }
    public string ViajeOrigen { get; set; } = string.Empty;
    public string ViajeDestino { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public string Criterio { get; set; } = CriteriosOptimizacion.PorDefecto;

    /// <summary>Envíos de la combinación que siguen disponibles, con su peso, desvío y pago.</summary>
    public List<OportunidadViewModel> Envios { get; set; } = [];

    /// <summary>Envíos pedidos que ya no están disponibles (los tomó otro conductor o dejaron de ser compatibles).</summary>
    public List<int> EnviosNoDisponibles { get; set; } = [];

    public string? Error { get; set; }

    public int CantidadPaquetes => Envios.Count;
    public decimal GananciaTotal => Envios.Sum(envio => envio.Pago);
    public decimal PesoTotalKg => Envios.Sum(envio => envio.PesoKg);
    public decimal DesvioTotalKm => Envios.Sum(envio => envio.DesvioKm);

    /// <summary>Solo se confirma si hay envíos, ninguno se perdió y no hubo ningún error.</summary>
    public bool PuedeConfirmar => Envios.Count > 0 && EnviosNoDisponibles.Count == 0 && Error is null;
}
