namespace RuteaYa.Entidades.Enums;

/// <summary>Días de la semana combinables (se guarda como un solo entero).</summary>
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
