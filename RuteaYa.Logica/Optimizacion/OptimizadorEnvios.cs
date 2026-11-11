using System.Globalization;
using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Optimizacion.Criterios;

namespace RuteaYa.Logica.Optimizacion;

/// <summary>
/// Elige la mejor combinación de envíos según un criterio (por defecto, máxima ganancia) respetando
/// cantidad máxima de paquetes, peso máximo y desvío máximo del viaje.
/// </summary>
/// <remarks>
/// Estrategia: búsqueda exhaustiva con poda (backtracking).
/// <list type="bullet">
/// <item>Se recorren todas las combinaciones de hasta <c>MaxPaquetes</c> envíos.</item>
/// <item>Peso y desvío solo crecen al agregar envíos, así que cuando una combinación supera un límite
/// se registra como descartada y no se extiende: cualquier combinación que la contenga también lo supera.</item>
/// <item>Solo se guardan las mejores alternativas y descartadas, por lo que la memoria es constante.</item>
/// </list>
/// Complejidad en el peor caso: la suma de C(n, i) para i = 1..MaxPaquetes. Para los volúmenes de un viaje
/// (decenas de candidatos y pocos paquetes) se resuelve en milisegundos y, a diferencia de una heurística,
/// garantiza el óptimo.
/// Qué combinación es mejor lo decide un <see cref="ICriterioOptimizacion"/> (patrón Strategy, HU-15).
/// Como la búsqueda recorre todas las combinaciones válidas, cualquier criterio obtiene su óptimo exacto.
/// El criterio por defecto es <see cref="CriterioMaximizarGanancias"/>: mayor ganancia y, si empatan,
/// menor desvío (regla del enunciado).
/// El desvío de una combinación es la suma de los desvíos de sus envíos (criterio del ejemplo del enunciado).
/// </remarks>
public sealed class OptimizadorEnvios : IOptimizadorEnvios
{
    public const int MaxAlternativas = 5;
    public const int MaxDescartadas = 10;

    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

    public ResultadoOptimizacion Optimizar(
        IReadOnlyCollection<CandidatoEnvio> candidatos,
        RestriccionesViaje restricciones,
        ICriterioOptimizacion? criterio = null)
    {
        Validar(candidatos, restricciones);

        var busqueda = new Busqueda(
            candidatos.OrderBy(c => c.EnvioId).ToArray(),
            restricciones,
            (criterio ?? CriterioMaximizarGanancias.Instancia).Comparar);
        busqueda.Explorar(desde: 0, pesoKg: 0m, desvioKm: 0m);

        return new ResultadoOptimizacion
        {
            MejorCombinacion = busqueda.Validas.FirstOrDefault(),
            Alternativas = busqueda.Validas.Skip(1).ToList(),
            Descartadas = busqueda.Descartadas,
            CombinacionesEvaluadas = busqueda.Evaluadas
        };
    }

    private static void Validar(IReadOnlyCollection<CandidatoEnvio>? candidatos, RestriccionesViaje? restricciones)
    {
        if (restricciones is null)
        {
            throw new ValidacionException("Las restricciones del viaje son obligatorias.");
        }

        if (restricciones.MaxPaquetes <= 0)
        {
            throw new ValidacionException("La cantidad máxima de paquetes debe ser mayor a cero.");
        }

        if (restricciones.PesoMaximoKg <= 0)
        {
            throw new ValidacionException("El peso máximo debe ser mayor a cero.");
        }

        if (restricciones.DesvioMaximoKm < 0)
        {
            throw new ValidacionException("El desvío máximo no puede ser negativo.");
        }

        if (candidatos is null)
        {
            throw new ValidacionException("La lista de envíos candidatos es obligatoria.");
        }

        var ids = new HashSet<int>();
        foreach (var candidato in candidatos)
        {
            if (candidato is null)
            {
                throw new ValidacionException("La lista de candidatos contiene un envío vacío.");
            }

            if (!ids.Add(candidato.EnvioId))
            {
                throw new ValidacionException($"El envío {candidato.EnvioId} está repetido en los candidatos.");
            }

            if (candidato.Pago <= 0)
            {
                throw new ValidacionException($"El pago del envío {candidato.EnvioId} debe ser mayor a cero.");
            }

            if (candidato.PesoKg <= 0)
            {
                throw new ValidacionException($"El peso del envío {candidato.EnvioId} debe ser mayor a cero.");
            }

            if (candidato.DesvioKm < 0)
            {
                throw new ValidacionException($"El desvío del envío {candidato.EnvioId} no puede ser negativo.");
            }
        }
    }

    /// <summary>Las descartadas se muestran de mayor a menor ganancia, sea cual sea el criterio.</summary>
    private static int CompararDescartadas(Combinacion a, Combinacion b)
    {
        var resultado = b.GananciaTotal.CompareTo(a.GananciaTotal);
        return resultado != 0 ? resultado : Desempate.CompararIds(a, b);
    }

    /// <summary>Inserta en una lista ordenada y la recorta a <paramref name="maximo"/> elementos.</summary>
    private static void InsertarAcotado(
        List<Combinacion> lista,
        Combinacion combinacion,
        int maximo,
        Comparison<Combinacion> comparar)
    {
        if (lista.Count == maximo && comparar(combinacion, lista[^1]) >= 0)
        {
            return;
        }

        var posicion = lista.FindIndex(existente => comparar(combinacion, existente) < 0);
        lista.Insert(posicion < 0 ? lista.Count : posicion, combinacion);

        if (lista.Count > maximo)
        {
            lista.RemoveAt(lista.Count - 1);
        }
    }

    private static List<string> MotivosDeDescarte(decimal pesoKg, decimal desvioKm, RestriccionesViaje restricciones)
    {
        var motivos = new List<string>();

        if (pesoKg > restricciones.PesoMaximoKg)
        {
            motivos.Add(string.Format(Cultura,
                "Supera el peso máximo: {0:0.##} kg de {1:0.##} kg.", pesoKg, restricciones.PesoMaximoKg));
        }

        if (desvioKm > restricciones.DesvioMaximoKm)
        {
            motivos.Add(string.Format(Cultura,
                "Supera el desvío máximo: {0:0.##} km de {1:0.##} km.", desvioKm, restricciones.DesvioMaximoKm));
        }

        return motivos;
    }

    /// <summary>Estado de una ejecución del backtracking.</summary>
    private sealed class Busqueda(
        CandidatoEnvio[] candidatos,
        RestriccionesViaje restricciones,
        Comparison<Combinacion> compararPorPreferencia)
    {
        private readonly List<CandidatoEnvio> _actual = new(restricciones.MaxPaquetes);

        // Mejor combinación + alternativas, ordenadas de mejor a peor.
        public List<Combinacion> Validas { get; } = new(MaxAlternativas + 1);
        public List<Combinacion> Descartadas { get; } = new(MaxDescartadas);
        public int Evaluadas { get; private set; }

        public void Explorar(int desde, decimal pesoKg, decimal desvioKm)
        {
            if (_actual.Count == restricciones.MaxPaquetes)
            {
                return;
            }

            for (var i = desde; i < candidatos.Length; i++)
            {
                var candidato = candidatos[i];
                var nuevoPeso = pesoKg + candidato.PesoKg;
                var nuevoDesvio = desvioKm + candidato.DesvioKm;

                _actual.Add(candidato);
                Evaluadas++;

                var motivos = MotivosDeDescarte(nuevoPeso, nuevoDesvio, restricciones);
                var combinacion = new Combinacion
                {
                    Envios = _actual.ToArray(),
                    GananciaTotal = _actual.Sum(c => c.Pago),
                    PesoTotalKg = nuevoPeso,
                    DesvioTotalKm = nuevoDesvio,
                    MotivosDescarte = motivos
                };

                if (combinacion.EsValida)
                {
                    InsertarAcotado(Validas, combinacion, MaxAlternativas + 1, compararPorPreferencia);
                    Explorar(i + 1, nuevoPeso, nuevoDesvio);
                }
                else
                {
                    // Poda: agregarle más envíos solo empeora peso y desvío.
                    InsertarAcotado(Descartadas, combinacion, MaxDescartadas, CompararDescartadas);
                }

                _actual.RemoveAt(_actual.Count - 1);
            }
        }
    }
}
