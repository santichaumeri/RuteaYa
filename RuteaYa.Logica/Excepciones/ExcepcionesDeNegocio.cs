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
