using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos.Historial;
using RuteaYa.Logica.Excepciones;

namespace RuteaYa.Logica.Servicios;

public sealed class HistorialService(RuteaYaDbContext dbContext) : IHistorialService
{
    public async Task<HistorialResumenDto> ObtenerHistorialAsync(
        int usuarioId,
        HistorialFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        ValidarFiltro(filtro);

        var asignaciones = dbContext.Asignaciones
            .AsNoTracking()
            .Where(x => x.Viaje.UsuarioId == usuarioId)
            .Where(x => !filtro.Desde.HasValue || x.Envio.Fecha >= filtro.Desde.Value)
            .Where(x => !filtro.Hasta.HasValue || x.Envio.Fecha <= filtro.Hasta.Value)
            .Where(x => !filtro.Estado.HasValue || x.Envio.Estado == filtro.Estado.Value);

        var todas = await asignaciones
            .Include(x => x.Viaje)
            .Include(x => x.Envio)
            .OrderByDescending(x => x.Envio.Fecha)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        var grupos = todas
            .GroupBy(x => x.ViajeId)
            .Select(CrearOperacion)
            .OrderByDescending(x => x.Periodo)
            .ThenBy(x => x.Ruta)
            .ToList();

        var entregadas = todas.Where(x => x.Envio.Estado == EstadoEnvio.Entregado).ToList();
        var viajesRealizados = todas
            .Where(x => x.Envio.Estado == EstadoEnvio.Entregado)
            .Select(x => x.ViajeId)
            .Distinct()
            .Count();
        var pagina = grupos
            .Skip((filtro.Pagina - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .ToList();

        return new HistorialResumenDto
        {
            ViajesRealizados = viajesRealizados,
            EnviosTransportados = entregadas.Count,
            GananciaEfectiva = entregadas.Sum(x => x.Ganancia),
            Operaciones = pagina,
            PaginaActual = filtro.Pagina,
            PageSize = filtro.PageSize,
            TotalRegistros = grupos.Count,
            TotalPaginas = (int)Math.Ceiling(grupos.Count / (double)filtro.PageSize)
        };
    }

    public async Task<HistorialDetalleDto> ObtenerDetalleAsync(
        int usuarioId,
        int viajeId,
        CancellationToken cancellationToken = default)
    {
        var viaje = await dbContext.Viajes
            .AsNoTracking()
            .Include(x => x.Asignaciones)
                .ThenInclude(x => x.Envio)
            .SingleOrDefaultAsync(x => x.Id == viajeId && x.UsuarioId == usuarioId, cancellationToken);

        if (viaje is null)
        {
            throw new NotFoundException("La operación no existe o no pertenece al usuario autenticado.");
        }

        return new HistorialDetalleDto
        {
            ViajeId = viaje.Id,
            Ruta = $"{viaje.Origen} → {viaje.Destino}",
            Envios = viaje.Asignaciones
                .OrderByDescending(x => x.Envio.Fecha)
                .Select(x => new HistorialEnvioDto
                {
                    EnvioId = x.EnvioId,
                    Fecha = x.Envio.Fecha,
                    Estado = x.Envio.Estado,
                    Ganancia = x.Ganancia
                })
                .ToList(),
            GananciaEfectiva = viaje.Asignaciones
                .Where(x => x.Envio.Estado == EstadoEnvio.Entregado)
                .Sum(x => x.Ganancia)
        };
    }

    private static HistorialOperacionDto CrearOperacion(IGrouping<int, Asignacion> grupo)
    {
        var asignaciones = grupo.ToList();
        var entregadas = asignaciones.Where(x => x.Envio.Estado == EstadoEnvio.Entregado).ToList();

        return new HistorialOperacionDto
        {
            ViajeId = grupo.Key,
            Ruta = $"{grupo.First().Viaje.Origen} → {grupo.First().Viaje.Destino}",
            Periodo = asignaciones.Max(x => x.Envio.Fecha),
            Estado = ObtenerEstado(asignaciones),
            CantidadPaquetes = asignaciones.Count,
            Monto = entregadas.Sum(x => x.Ganancia)
        };
    }

    private static EstadoEnvio ObtenerEstado(IReadOnlyCollection<Asignacion> asignaciones)
    {
        if (asignaciones.All(x => x.Envio.Estado == EstadoEnvio.Entregado))
        {
            return EstadoEnvio.Entregado;
        }

        if (asignaciones.Any(x => x.Envio.Estado is EstadoEnvio.Retirado or EstadoEnvio.EnCamino))
        {
            return EstadoEnvio.EnCamino;
        }

        if (asignaciones.Any(x => x.Envio.Estado == EstadoEnvio.ConductorAsignado))
        {
            return EstadoEnvio.ConductorAsignado;
        }

        return EstadoEnvio.Cancelado;
    }

    private static void ValidarFiltro(HistorialFiltroDto filtro)
    {
        if (filtro is null)
        {
            throw new ValidacionException("El filtro es obligatorio.");
        }

        if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde > filtro.Hasta)
        {
            throw new ValidacionException("La fecha desde no puede ser posterior a la fecha hasta.");
        }

        if (filtro.Pagina < 1 || filtro.PageSize is < 1 or > 100)
        {
            throw new ValidacionException("La paginación no es válida.");
        }
    }
}
