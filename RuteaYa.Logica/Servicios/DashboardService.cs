using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos.Dashboard;
using RuteaYa.Logica.Optimizacion.Criterios;

namespace RuteaYa.Logica.Servicios;

/// <summary>
/// Resumen de la pantalla principal (HU-03). El potencial del mes reutiliza el cálculo de HU-20,
/// así la tarjeta del dashboard y la pantalla 14 muestran siempre el mismo número.
/// </summary>
public sealed class DashboardService(
    RuteaYaDbContext dbContext,
    IPotencialMensualService potencialMensualService,
    TimeProvider reloj) : IDashboardService
{
    public async Task<DashboardResumenDto?> ObtenerResumenAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await dbContext.Usuarios
            .AsNoTracking()
            .Where(x => x.Id == usuarioId)
            .Select(x => new { x.Nombre })
            .SingleOrDefaultAsync(cancellationToken);

        if (usuario is null)
        {
            return null;
        }

        var viajesData = await dbContext.Viajes
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && x.Estado == EstadoViaje.Activo)
            .OrderBy(x => x.HoraSalida)
            .Take(5)
            .Select(x => new
            {
                Id = x.Id,
                Ruta = $"{x.Origen} → {x.Destino}",
                x.DiasSemana,
                Horario = $"{x.HoraSalida:HH\\:mm} – {x.HoraLlegada:HH\\:mm}",
                Estado = x.Estado.ToString()
            })
            .ToListAsync(cancellationToken);

        var viajes = viajesData
            .Select(x => new ViajeResumenDto
            {
                Id = x.Id,
                Ruta = x.Ruta,
                Dias = FormatearDias(x.DiasSemana),
                Horario = x.Horario,
                Estado = x.Estado
            })
            .ToList();

        var envios = await dbContext.Envios
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId
                && x.Estado != EstadoEnvio.Entregado
                && x.Estado != EstadoEnvio.Cancelado)
            .OrderBy(x => x.Fecha)
            .Take(5)
            .Select(x => new EnvioResumenDto
            {
                Id = x.Id,
                Ruta = $"{x.Origen} → {x.Destino}",
                Descripcion = x.Descripcion,
                MontoOfrecido = x.MontoOfrecido,
                Estado = x.Estado.ToString()
            })
            .ToListAsync(cancellationToken);

        var cantidadViajes = await dbContext.Viajes
            .CountAsync(x => x.UsuarioId == usuarioId && x.Estado == EstadoViaje.Activo, cancellationToken);
        var cantidadEnvios = await dbContext.Envios
            .CountAsync(x => x.UsuarioId == usuarioId
                && x.Estado != EstadoEnvio.Entregado
                && x.Estado != EstadoEnvio.Cancelado, cancellationToken);

        // Mes en curso con el criterio por defecto, igual que la pantalla 14 al abrirse.
        var hoy = reloj.GetLocalNow();
        var potencial = await potencialMensualService.CalcularAsync(
            usuarioId, hoy.Year, hoy.Month, CriterioOptimizacion.MaximizarGanancias, cancellationToken);

        return new DashboardResumenDto
        {
            NombreUsuario = usuario.Nombre,
            ProximosViajes = cantidadViajes,
            EnviosPendientes = cantidadEnvios,
            Oportunidades = 0,
            PotencialMes = potencial.GananciaPotencial,
            Viajes = viajes,
            Envios = envios
        };
    }

    private static string FormatearDias(DiasSemana dias)
    {
        var nombres = new[]
        {
            (DiasSemana.Lunes, "Lunes"),
            (DiasSemana.Martes, "Martes"),
            (DiasSemana.Miercoles, "Miércoles"),
            (DiasSemana.Jueves, "Jueves"),
            (DiasSemana.Viernes, "Viernes"),
            (DiasSemana.Sabado, "Sábado"),
            (DiasSemana.Domingo, "Domingo")
        };

        var seleccionados = nombres
            .Where(x => dias.HasFlag(x.Item1))
            .Select(x => x.Item2)
            .ToList();

        return seleccionados.Count switch
        {
            0 => "Sin días",
            1 => seleccionados[0],
            2 => $"{seleccionados[0]} y {seleccionados[1]}",
            _ => $"{string.Join(", ", seleccionados[..^1])} y {seleccionados[^1]}"
        };
    }
}
