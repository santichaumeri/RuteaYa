using System.ComponentModel.DataAnnotations;
using RuteaYa.Logica.Optimizacion.Criterios;

namespace RuteaYa.Logica.Dtos.Optimizacion;

/// <summary>
/// Límites del viaje y envíos candidatos a combinar.
/// Los candidatos tienen que venir ya filtrados por compatibilidad (fecha, día y orden del recorrido).
/// </summary>
public sealed class OptimizarRequest
{
    /// <example>3</example>
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad máxima de paquetes debe ser mayor a cero.")]
    public int MaxPaquetes { get; set; }

    /// <example>10</example>
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ParseLimitsInInvariantCulture = true, ErrorMessage = "El peso máximo debe ser mayor a cero.")]
    public decimal PesoMaximoKg { get; set; }

    /// <example>5</example>
    [Range(typeof(decimal), "0", "79228162514264337593543950335", ParseLimitsInInvariantCulture = true, ErrorMessage = "El desvío máximo no puede ser negativo.")]
    public decimal DesvioMaximoKm { get; set; }

    /// <summary>
    /// MaximizarGanancias (por defecto), MinimizarDesvios o Equilibrio.
    /// Acepta el nombre o el número (1, 2, 3).
    /// </summary>
    /// <example>MaximizarGanancias</example>
    public CriterioOptimizacion Criterio { get; set; } = CriterioOptimizacion.MaximizarGanancias;

    [Required(ErrorMessage = "La lista de candidatos es obligatoria.")]
    public List<CandidatoEnvioRequest> Candidatos { get; set; } = [];
}

public sealed class CandidatoEnvioRequest
{
    /// <example>1</example>
    [Range(1, int.MaxValue, ErrorMessage = "El id del envío no es válido.")]
    public int EnvioId { get; set; }

    /// <example>4000</example>
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ParseLimitsInInvariantCulture = true, ErrorMessage = "El pago debe ser mayor a cero.")]
    public decimal Pago { get; set; }

    /// <example>2</example>
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ParseLimitsInInvariantCulture = true, ErrorMessage = "El peso debe ser mayor a cero.")]
    public decimal PesoKg { get; set; }

    /// <example>1</example>
    [Range(typeof(decimal), "0", "79228162514264337593543950335", ParseLimitsInInvariantCulture = true, ErrorMessage = "El desvío no puede ser negativo.")]
    public decimal DesvioKm { get; set; }
}
