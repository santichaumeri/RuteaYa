using RuteaYa.Logica.Excepciones;

namespace RuteaYa.Logica.Servicios;

/// <summary>
/// Reglas de las paradas intermedias de un viaje (pantalla 4). Las usan el alta y la edición del viaje
/// y los endpoints de paradas, así una parada se valida igual venga por donde venga.
/// </summary>
internal static class ReglasParadas
{
    /// <summary>
    /// Valida la lista completa de paradas, en orden: ninguna vacía, ninguna igual al origen o al destino
    /// y sin repetidas. Lanza ValidacionException (400) con el primer problema que encuentra.
    /// </summary>
    public static void Validar(
        IReadOnlyCollection<string> paradas,
        string origen,
        string destino)
    {
        var direcciones = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var parada in paradas)
        {
            if (parada.Length == 0)
            {
                throw new ValidacionException("La dirección de cada parada es obligatoria.");
            }

            if (string.Equals(parada, origen, StringComparison.OrdinalIgnoreCase)
                || string.Equals(parada, destino, StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidacionException(
                    "Una parada no puede coincidir con el origen ni con el destino.");
            }

            if (!direcciones.Add(parada))
            {
                throw new ValidacionException("No se pueden repetir direcciones de paradas.");
            }
        }
    }
}
