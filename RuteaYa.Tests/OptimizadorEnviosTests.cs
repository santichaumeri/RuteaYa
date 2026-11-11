using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Optimizacion;
using Xunit;

namespace RuteaYa.Tests;

public class OptimizadorEnviosTests
{
    private readonly OptimizadorEnvios _optimizador = new();

    // Ejemplo de la sección 12 del enunciado.
    private static readonly CandidatoEnvio A = new(1, 4000m, 2m, 1.0m);
    private static readonly CandidatoEnvio B = new(2, 5500m, 6m, 2.0m);
    private static readonly CandidatoEnvio C = new(3, 3500m, 4m, 1.0m);
    private static readonly CandidatoEnvio D = new(4, 7000m, 8m, 3.0m);
    private static readonly CandidatoEnvio E = new(5, 2500m, 1m, 0.5m);
    private static readonly RestriccionesViaje LimitesEnunciado = new(3, 10m, 5m);

    [Fact]
    public void Caso_del_enunciado_elige_A_B_E()
    {
        var resultado = _optimizador.Optimizar([A, B, C, D, E], LimitesEnunciado);

        var mejor = Assert.IsType<Combinacion>(resultado.MejorCombinacion);
        Assert.Equal([1, 2, 5], mejor.EnvioIds);
        Assert.Equal(12000m, mejor.GananciaTotal);
        Assert.Equal(9m, mejor.PesoTotalKg);
        Assert.Equal(3.5m, mejor.DesvioTotalKm);
        Assert.Equal(3, mejor.CantidadPaquetes);
    }

    [Fact]
    public void Caso_del_enunciado_devuelve_alternativas_ordenadas()
    {
        var resultado = _optimizador.Optimizar([A, B, C, D, E], LimitesEnunciado);

        Assert.Equal([1, 4], resultado.Alternativas[0].EnvioIds);        // A+D $11.000
        Assert.Equal(11000m, resultado.Alternativas[0].GananciaTotal);
        Assert.Equal([1, 3, 5], resultado.Alternativas[1].EnvioIds);     // A+C+E $10.000
        Assert.Equal(10000m, resultado.Alternativas[1].GananciaTotal);
        Assert.True(resultado.Alternativas.Count <= OptimizadorEnvios.MaxAlternativas);
        Assert.All(resultado.Alternativas, a => Assert.True(a.EsValida));
    }

    [Fact]
    public void Caso_del_enunciado_informa_descartes_con_motivo()
    {
        var resultado = _optimizador.Optimizar([A, B, C, D, E], LimitesEnunciado);

        Assert.NotEmpty(resultado.Descartadas);
        Assert.All(resultado.Descartadas, d => Assert.NotEmpty(d.MotivosDescarte));

        // A+B+C gana $13.000 pero pesa 12 kg.
        var abc = Assert.Single(resultado.Descartadas, d => d.EnvioIds.SequenceEqual([1, 2, 3]));
        Assert.Contains(abc.MotivosDescarte, m => m.Contains("peso máximo") && m.Contains("12 kg"));
    }

    [Fact]
    public void Empate_de_ganancia_prefiere_menor_desvio()
    {
        var lejos = new CandidatoEnvio(1, 5000m, 1m, 3m);
        var cerca = new CandidatoEnvio(2, 5000m, 1m, 1m);

        var resultado = _optimizador.Optimizar([lejos, cerca], new RestriccionesViaje(1, 10m, 5m));

        Assert.Equal([2], resultado.MejorCombinacion!.EnvioIds);
        Assert.Equal([1], resultado.Alternativas[0].EnvioIds);
    }

    [Fact]
    public void Empate_de_ganancia_entre_combinaciones_prefiere_menor_desvio_total()
    {
        // X+Y y Z ganan lo mismo ($6.000); Z desvía menos.
        var x = new CandidatoEnvio(1, 3000m, 1m, 2m);
        var y = new CandidatoEnvio(2, 3000m, 1m, 2m);
        var z = new CandidatoEnvio(3, 6000m, 5m, 1m);

        var resultado = _optimizador.Optimizar([x, y, z], new RestriccionesViaje(2, 5m, 10m));

        Assert.Equal([3], resultado.MejorCombinacion!.EnvioIds);
    }

    [Fact]
    public void Sin_candidatos_no_hay_combinacion()
    {
        var resultado = _optimizador.Optimizar([], LimitesEnunciado);

        Assert.Null(resultado.MejorCombinacion);
        Assert.Empty(resultado.Alternativas);
        Assert.Empty(resultado.Descartadas);
        Assert.Equal(0, resultado.CombinacionesEvaluadas);
    }

    [Fact]
    public void Si_ningun_envio_entra_no_hay_combinacion_y_se_explica_por_que()
    {
        var pesado = new CandidatoEnvio(1, 9000m, 20m, 1m);
        var lejano = new CandidatoEnvio(2, 9000m, 1m, 50m);

        var resultado = _optimizador.Optimizar([pesado, lejano], LimitesEnunciado);

        Assert.Null(resultado.MejorCombinacion);
        Assert.Empty(resultado.Alternativas);
        Assert.Equal(2, resultado.Descartadas.Count);
        Assert.Contains(resultado.Descartadas, d => d.MotivosDescarte.Any(m => m.Contains("peso máximo")));
        Assert.Contains(resultado.Descartadas, d => d.MotivosDescarte.Any(m => m.Contains("desvío máximo")));
    }

    [Fact]
    public void Un_solo_envio_valido_es_la_recomendacion()
    {
        var resultado = _optimizador.Optimizar([E], LimitesEnunciado);

        Assert.Equal([5], resultado.MejorCombinacion!.EnvioIds);
        Assert.Empty(resultado.Alternativas);
        Assert.Equal(1, resultado.CombinacionesEvaluadas);
    }

    [Fact]
    public void Combinacion_que_supera_peso_y_desvio_informa_ambos_motivos()
    {
        var grande = new CandidatoEnvio(1, 1000m, 15m, 9m);

        var resultado = _optimizador.Optimizar([grande], LimitesEnunciado);

        var descartada = Assert.Single(resultado.Descartadas);
        Assert.Equal(2, descartada.MotivosDescarte.Count);
    }

    [Fact]
    public void Nunca_supera_la_cantidad_maxima_de_paquetes()
    {
        var livianos = Enumerable.Range(1, 6)
            .Select(i => new CandidatoEnvio(i, 1000m * i, 0.5m, 0.1m))
            .ToList();

        var resultado = _optimizador.Optimizar(livianos, new RestriccionesViaje(2, 100m, 100m));

        Assert.Equal([5, 6], resultado.MejorCombinacion!.EnvioIds);
        Assert.All(resultado.Alternativas, a => Assert.True(a.CantidadPaquetes <= 2));
    }

    [Fact]
    public void El_resultado_no_depende_del_orden_de_los_candidatos()
    {
        var resultado1 = _optimizador.Optimizar([A, B, C, D, E], LimitesEnunciado);
        var resultado2 = _optimizador.Optimizar([E, D, C, B, A], LimitesEnunciado);

        Assert.Equal(resultado1.MejorCombinacion!.EnvioIds, resultado2.MejorCombinacion!.EnvioIds);
        Assert.Equal(
            resultado1.Alternativas.Select(a => string.Join(",", a.EnvioIds)),
            resultado2.Alternativas.Select(a => string.Join(",", a.EnvioIds)));
    }

    [Fact]
    public void Coincide_con_fuerza_bruta_en_casos_aleatorios()
    {
        var random = new Random(2026);

        for (var caso = 0; caso < 300; caso++)
        {
            var cantidad = random.Next(0, 9);
            var candidatos = Enumerable.Range(1, cantidad)
                .Select(id => new CandidatoEnvio(
                    id,
                    random.Next(1, 10) * 500m,
                    random.Next(1, 8),
                    random.Next(0, 6) * 0.5m))
                .ToList();
            var limites = new RestriccionesViaje(random.Next(1, 5), random.Next(1, 15), random.Next(0, 8));

            var esperado = MejorPorFuerzaBruta(candidatos, limites);
            var obtenido = _optimizador.Optimizar(candidatos, limites).MejorCombinacion;

            if (esperado is null)
            {
                Assert.Null(obtenido);
                continue;
            }

            Assert.NotNull(obtenido);
            Assert.Equal(esperado.Value.Ganancia, obtenido.GananciaTotal);
            Assert.Equal(esperado.Value.Desvio, obtenido.DesvioTotalKm);
            Assert.True(obtenido.CantidadPaquetes <= limites.MaxPaquetes);
            Assert.True(obtenido.PesoTotalKg <= limites.PesoMaximoKg);
            Assert.True(obtenido.DesvioTotalKm <= limites.DesvioMaximoKm);
        }
    }

    [Fact]
    public void Envio_repetido_lanza_ValidacionException()
    {
        Assert.Throws<ValidacionException>(() => _optimizador.Optimizar([A, A], LimitesEnunciado));
    }

    [Theory]
    [InlineData(0, 10, 5)]
    [InlineData(3, 0, 5)]
    [InlineData(3, 10, -1)]
    public void Restricciones_invalidas_lanzan_ValidacionException(int maxPaquetes, int pesoMaximo, int desvioMaximo)
    {
        var limites = new RestriccionesViaje(maxPaquetes, pesoMaximo, desvioMaximo);

        Assert.Throws<ValidacionException>(() => _optimizador.Optimizar([A], limites));
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1000, 0, 1)]
    [InlineData(1000, 1, -1)]
    public void Candidato_invalido_lanza_ValidacionException(int pago, int peso, int desvio)
    {
        var candidato = new CandidatoEnvio(1, pago, peso, desvio);

        Assert.Throws<ValidacionException>(() => _optimizador.Optimizar([candidato], LimitesEnunciado));
    }

    // Recorre los 2^n subconjuntos sin poda: referencia para validar el backtracking.
    private static (decimal Ganancia, decimal Desvio)? MejorPorFuerzaBruta(
        IReadOnlyList<CandidatoEnvio> candidatos,
        RestriccionesViaje limites)
    {
        (decimal Ganancia, decimal Desvio)? mejor = null;

        for (var mascara = 1; mascara < 1 << candidatos.Count; mascara++)
        {
            var elegidos = candidatos.Where((_, i) => (mascara & (1 << i)) != 0).ToList();
            var ganancia = elegidos.Sum(c => c.Pago);
            var peso = elegidos.Sum(c => c.PesoKg);
            var desvio = elegidos.Sum(c => c.DesvioKm);

            if (elegidos.Count > limites.MaxPaquetes || peso > limites.PesoMaximoKg || desvio > limites.DesvioMaximoKm)
            {
                continue;
            }

            if (mejor is null
                || ganancia > mejor.Value.Ganancia
                || (ganancia == mejor.Value.Ganancia && desvio < mejor.Value.Desvio))
            {
                mejor = (ganancia, desvio);
            }
        }

        return mejor;
    }
}
