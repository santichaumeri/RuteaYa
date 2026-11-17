namespace RuteaYa.Web.Models;

// Copias de los enums de la API para el MVC, que no referencia a RuteaYa.Entidades ni a RuteaYa.Logica.
// La API los manda como números en el JSON: los valores tienen que coincidir con los de RuteaYa.Entidades.Enums.

public enum EstadoEnvio
{
    BuscandoConductor = 1,
    ConductorAsignado = 2,
    Retirado = 3,
    EnCamino = 4,
    Entregado = 5,
    Cancelado = 6
}

/// <summary>Días de la semana combinables: la API los manda como un solo entero.</summary>
[Flags]
public enum DiasSemana
{
    Ninguno = 0,
    Lunes = 1,
    Martes = 2,
    Miercoles = 4,
    Jueves = 8,
    Viernes = 16,
    Sabado = 32,
    Domingo = 64
}

public enum EstadoViaje
{
    Activo = 1,
    Inactivo = 2
}

public enum TamanoPaquete
{
    Pequeno = 1,
    Mediano = 2,
    Grande = 3
}
