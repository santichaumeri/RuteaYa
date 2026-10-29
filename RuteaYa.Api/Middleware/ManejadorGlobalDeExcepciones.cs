using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuteaYa.Logica.Excepciones;

namespace RuteaYa.Api.Middleware;

/// <summary>
/// Atrapa cualquier excepción no controlada y la convierte en una respuesta HTTP uniforme (ProblemDetails).
/// </summary>
public class ManejadorGlobalDeExcepciones : IExceptionHandler
{
    private readonly ILogger<ManejadorGlobalDeExcepciones> _logger;

    public ManejadorGlobalDeExcepciones(ILogger<ManejadorGlobalDeExcepciones> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, titulo) = exception switch
        {
            ValidacionException => (StatusCodes.Status400BadRequest, "Solicitud inválida"),
            NoAutenticadoException => (StatusCodes.Status401Unauthorized, "No autenticado"),
            AccesoDenegadoException => (StatusCodes.Status403Forbidden, "Acceso denegado"),
            NotFoundException => (StatusCodes.Status404NotFound, "No encontrado"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflicto"),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflicto de concurrencia"),
            _ => (StatusCodes.Status500InternalServerError, "Error interno")
        };

        string detalle;
        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Error no controlado en {Ruta}", httpContext.Request.Path);
            detalle = "Ocurrió un error inesperado.";   // no se filtran detalles internos
        }
        else
        {
            _logger.LogWarning("{Titulo}: {Mensaje}", titulo, exception.Message);
            detalle = exception is DbUpdateConcurrencyException
                ? "El recurso fue modificado por otro usuario. Volvé a intentar."
                : exception.Message;
        }

        var problema = new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = detalle,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problema, cancellationToken);
        return true;
    }
}
