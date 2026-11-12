using RuteaYa.Entidades.Enums;

namespace RuteaYa.Logica.Excepciones;

/// <summary>Datos inválidos. La API responde 400.</summary>
public class ValidacionException : Exception
{
    public ValidacionException(string mensaje) : base(mensaje) { }
}

/// <summary>Credenciales inválidas o sin sesión. La API responde 401.</summary>
public class NoAutenticadoException : Exception
{
    public NoAutenticadoException(string mensaje) : base(mensaje) { }
}

/// <summary>El recurso pertenece a otro usuario. La API responde 403.</summary>
public class AccesoDenegadoException : Exception
{
    public AccesoDenegadoException(string mensaje) : base(mensaje) { }
}

/// <summary>El recurso no existe. La API responde 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string mensaje) : base(mensaje) { }
}

/// <summary>Choca con el estado actual (email repetido, envío ya asignado, transición inválida). La API responde 409.</summary>
public class ConflictException : Exception
{
    public ConflictException(string mensaje) : base(mensaje) { }
}

/// <summary>
/// Al confirmar una combinación, uno o más envíos ya no están disponibles (los tomó otro conductor
/// o se cancelaron). No se reservó ninguno. La API responde 409 e informa los ids (HU-17).
/// </summary>
public class CombinacionConflictoException : Exception
{
    public IReadOnlyList<int> EnvioIdsNoDisponibles { get; }

    public CombinacionConflictoException(IReadOnlyList<int> envioIdsNoDisponibles)
        : base(ArmarMensaje(envioIdsNoDisponibles))
    {
        EnvioIdsNoDisponibles = envioIdsNoDisponibles;
    }

    private static string ArmarMensaje(IReadOnlyList<int> ids) =>
        $"No se pudo reservar la combinación: no están disponibles los envíos {string.Join(", ", ids.Select(id => $"#{id}"))}. No se reservó ninguno.";
}

public class TransicionEstadoInvalidaException : InvalidOperationException
{
    public EstadoEnvio EstadoActual { get; }
    public EstadoEnvio EstadoNuevo { get; }

    public TransicionEstadoInvalidaException(EstadoEnvio EstadoActual, EstadoEnvio EstadoNuevo) : base($"No está permitida la transición de estado {EstadoActual} a {EstadoNuevo}.")
    {
        this.EstadoActual = EstadoActual;
        this.EstadoNuevo = EstadoNuevo;
    }
}