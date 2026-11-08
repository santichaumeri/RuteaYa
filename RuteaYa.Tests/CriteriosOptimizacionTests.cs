using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Optimizacion;
using RuteaYa.Logica.Optimizacion.Criterios;
using Xunit;

namespace RuteaYa.Tests;

public class CriteriosOptimizacionTests
{
    private readonly OptimizadorEnvios _optimizador = new();

    // Ejemplo de la sección 12 del enunciado.
    private static readonly CandidatoEnvio A = new(1, 4000m, 2m, 1.0m);
    private static readonly CandidatoEnvio B = new(2, 5500m, 6m, 2.0m);
    private static readonly CandidatoEnvio C = new(3, 3500m, 4m, 1.0m);
    private static readonly CandidatoEnvio D = new(4, 7000m, 8m, 3.0m);
    private static readonly CandidatoEnvio E = new(5, 2500m, 1m, 0.5m);
    private static readonly CandidatoEnvio[] Enunciado = [A, B, C, D, E];
    private static readonly RestriccionesViaje LimitesEnunciado = new(3, 10m, 5m);

    [Fact]
    public void Mismo_conjunto_da_combinaciones_distintas_segun_el_criterio()
    {
        var porGanancia = _optimizador.Optimizar(Enunciado, LimitesEnunciado, CriterioMaximizarGanancias.Instancia);
        var porDesvio = _optimizador.Optimizar(Enunciado, LimitesEnunciado, new CriterioMinimizarDesvios());
        var porEquilibrio = _optimizador.Optimizar(Enunciado, LimitesEnunciado, new CriterioEquilibrio(2500m));

        // A+B+E: $12.000 con 3,5 km.
        Assert.Equal([1, 2, 5], porGanancia.MejorCombinacion!.EnvioIds);
        // E solo: 0,5 km, el menor desvío posible.
        Assert.Equal([5], porDesvio.MejorCombinacion!.EnvioIds);
        // A+C+E: $10.000 − $2.500 × 2,5 km = $3.750, contra A+B+E: $12.000 − $2.500 × 3,5 km = $3.250.
        Assert.Equal([1, 3, 5], porEquilibrio.MejorCombinacion!.EnvioIds);
    }

    [Fact]
    public void Sin_criterio_se_usa_maximizar_ganancias()
    {
        var sinCriterio = _optimizador.Optimizar(Enunciado, LimitesEnunciado);
        var porGanancia = _optimizador.Optimizar(Enunciado, LimitesEnunciado, CriterioMaximizarGanancias.Instancia);

        Assert.Equal(porGanancia.MejorCombinacion!.EnvioIds, sinCriterio.MejorCombinacion!.EnvioIds);
        Assert.Equal(
            porGanancia.Alternativas.Select(x => string.Join(",", x.EnvioIds)),
            sinCriterio.Alternativas.Select(x => string.Join(",", x.EnvioIds)));
    }

    [Fact]
    public void Maximizar_ganancias_desempata_por_menor_desvio()
    {
        var lejos = new CandidatoEnvio(1, 5000m, 1m, 3m);
        var cerca = new CandidatoEnvio(2, 5000m, 1m, 1m);

        var resultado = _optimizador.Optimizar([lejos, cerca], new RestriccionesViaje(1, 10m, 5m), CriterioMaximizarGanancias.Instancia);

        Assert.Equal([2], resultado.MejorCombinacion!.EnvioIds);
    }

    [Fact]
    public void Minimizar_desvios_suma_todos_los_envios_que_no_desvian()
    {
        var enCamino1 = new CandidatoEnvio(1, 1000m, 1m, 0m);
        var enCamino2 = new CandidatoEnvio(2, 1500m, 1m, 0m);
        var lejano = new CandidatoEnvio(3, 9000m, 1m, 2m);

        var resultado = _optimizador.Optimizar([enCamino1, enCamino2, lejano], new RestriccionesViaje(3, 10m, 5m), new CriterioMinimizarDesvios());

        Assert.Equal([1, 2], resultado.MejorCombinacion!.EnvioIds);
        Assert.Equal(0m, resultado.MejorCombinacion.DesvioTotalKm);
    }

    [Fact]
    public void Equilibrio_deja_afuera_el_envio_que_paga_menos_que_su_desvio()
    {
        var conveniente = new CandidatoEnvio(1, 5000m, 1m, 1m);   // neto $4.000
        var inconveniente = new CandidatoEnvio(2, 1500m, 1m, 2m); // neto −$500

        var resultado = _optimizador.Optimizar([conveniente, inconveniente], new RestriccionesViaje(2, 10m, 5m), new CriterioEquilibrio(1000m));

        Assert.Equal([1], resultado.MejorCombinacion!.EnvioIds);
    }

    [Fact]
    public void Equilibrio_con_costo_cero_equivale_a_maximizar_ganancias()
    {
        var equilibrio = _optimizador.Optimizar(Enunciado, LimitesEnunciado, new CriterioEquilibrio(0m));

        Assert.Equal([1, 2, 5], equilibrio.MejorCombinacion!.EnvioIds);
    }

    [Fact]
    public void Equilibrio_rechaza_costo_negativo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CriterioEquilibrio(-1m));
    }

    [Theory]
    [MemberData(nameof(TodosLosCriterios))]
    public void Las_alternativas_quedan_ordenadas_segun_el_criterio(ICriterioOptimizacion criterio)
    {
        var resultado = _optimizador.Optimizar(Enunciado, LimitesEnunciado, criterio);
        var ordenadas = new[] { resultado.MejorCombinacion! }.Concat(resultado.Alternativas).ToList();

        for (var i = 1; i < ordenadas.Count; i++)
        {
            Assert.True(criterio.Comparar(ordenadas[i - 1], ordenadas[i]) < 0);
        }
    }

    [Theory]
    [MemberData(nameof(TodosLosCriterios))]
    public void Cada_criterio_encuentra_su_optimo_en_casos_aleatorios(ICriterioOptimizacion criterio)
    {
        var random = new Random(2026);

        for (var caso = 0; caso < 200; caso++)
        {
            var candidatos = Enumerable.Range(1, random.Next(1, 9))
                .Select(id => new CandidatoEnvio(id, random.Next(1, 10) * 500m, random.Next(1, 8), random.Next(0, 6) * 0.5m))
                .ToList();
            var limites = new RestriccionesViaje(random.Next(1, 5), random.Next(1, 15), random.Next(0, 8));

            var esperado = MejorPorFuerzaBruta(candidatos, limites, criterio);
            var obtenido = _optimizador.Optimizar(candidatos, limites, criterio).MejorCombinacion;

            if (esperado is null)
            {
                Assert.Null(obtenido);
                continue;
            }

            Assert.NotNull(obtenido);
            Assert.Equal(0, criterio.Comparar(esperado, obtenido));
        }
    }

    [Fact]
    public void El_catalogo_devuelve_cada_estrategia_y_rechaza_un_criterio_inexistente()
    {
        var catalogo = new CatalogoCriterios(
            [CriterioMaximizarGanancias.Instancia, new CriterioMinimizarDesvios(), new CriterioEquilibrio()]);

        Assert.IsType<CriterioMaximizarGanancias>(catalogo.Obtener(CriterioOptimizacion.MaximizarGanancias));
        Assert.IsType<CriterioMinimizarDesvios>(catalogo.Obtener(CriterioOptimizacion.MinimizarDesvios));
        Assert.IsType<CriterioEquilibrio>(catalogo.Obtener(CriterioOptimizacion.Equilibrio));
        Assert.Throws<ValidacionException>(() => catalogo.Obtener((CriterioOptimizacion)99));
    }

    public static TheoryData<ICriterioOptimizacion> TodosLosCriterios() =>
    [
        CriterioMaximizarGanancias.Instancia,
        new CriterioMinimizarDesvios(),
        new CriterioEquilibrio(1000m)
    ];

    // Recorre los 2^n subconjuntos sin poda: referencia para validar el backtracking con cada criterio.
    private static Combinacion? MejorPorFuerzaBruta(
        IReadOnlyList<CandidatoEnvio> candidatos,
        RestriccionesViaje limites,
        ICriterioOptimizacion criterio)
    {
        Combinacion? mejor = null;

        for (var mascara = 1; mascara < 1 << candidatos.Count; mascara++)
        {
            var elegidos = candidatos.Where((_, i) => (mascara & (1 << i)) != 0).ToArray();
            var combinacion = new Combinacion
            {
                Envios = elegidos,
                GananciaTotal = elegidos.Sum(c => c.Pago),
                PesoTotalKg = elegidos.Sum(c => c.PesoKg),
                DesvioTotalKm = elegidos.Sum(c => c.DesvioKm),
                MotivosDescarte = []
            };

            if (elegidos.Length > limites.MaxPaquetes
                || combinacion.PesoTotalKg > limites.PesoMaximoKg
                || combinacion.DesvioTotalKm > limites.DesvioMaximoKm)
            {
                continue;
            }

            if (mejor is null || criterio.Comparar(combinacion, mejor) < 0)
            {
                mejor = combinacion;
            }
        }

        return mejor;
    }
}
