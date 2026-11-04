using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos.Dashboard;

namespace RuteaYa.Logica.Servicios;

public sealed class DashboardService(RuteaYaDbContext dbContext) : IDashboardService
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

        var viajes = await dbContext.Viajes
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && x.Estado == EstadoViaje.Activo)
            .OrderBy(x => x.HoraSalida)
            .Take(5)
            .Select(x => new ViajeResumenDto
            {
                Id = x.Id,
                Ruta = $"{x.Origen} → {x.Destino}",
                Dias = x.DiasSemana.ToString(),
                Horario = $"{x.HoraSalida:HH\\:mm} – {x.HoraLlegada:HH\\:mm}",
                Estado = x.Estado.ToString()
            })
            .ToListAsync(cancellationToken);

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

        return new DashboardResumenDto
        {
            NombreUsuario = usuario.Nombre,
            ProximosViajes = cantidadViajes,
            EnviosPendientes = cantidadEnvios,
            Oportunidades = 0,
            PotencialMes = 0,
            Viajes = viajes,
            Envios = envios
        };
    }
}
