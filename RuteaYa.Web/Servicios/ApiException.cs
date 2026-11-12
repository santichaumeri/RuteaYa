namespace RuteaYa.Web.Servicios;

/// <summary>La API respondió con un error. Trae el código HTTP y el mensaje.</summary>
public class ApiException : Exception
{
    public int StatusCode { get; }

    /// <summary>
    /// Ids de los envíos que ya no están disponibles cuando la API rechaza una combinación con 409 (HU-17).
    /// Vacía en cualquier otro error.
    /// </summary>
    public IReadOnlyList<int> EnviosNoDisponibles { get; }

    public ApiException(int statusCode, string mensaje, IReadOnlyList<int>? enviosNoDisponibles = null) : base(mensaje)
    {
        StatusCode = statusCode;
        EnviosNoDisponibles = enviosNoDisponibles ?? [];
    }
}
