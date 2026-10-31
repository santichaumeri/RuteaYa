namespace RuteaYa.Web.Servicios;

/// <summary>La API respondió con un error. Trae el código HTTP y el mensaje.</summary>
public class ApiException : Exception
{
    public int StatusCode { get; }

    public ApiException(int statusCode, string mensaje) : base(mensaje)
    {
        StatusCode = statusCode;
    }
}
