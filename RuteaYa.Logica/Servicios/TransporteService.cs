using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos.Transportes;
using RuteaYa.Logica.Excepciones;

namespace RuteaYa.Logica.Servicios;

/// <summary>Seguimiento del conductor sobre los paquetes que aceptó llevar (HU-18).</summary>
public class TransporteService : ITransporteService
{
    private readonly RuteaYaDbContext _dbContext;
    private readonly IEstadoEnvioService _estadoEnvioService;

    public TransporteService(RuteaYaDbContext dbContext, IEstadoEnvioService estadoEnvioService)
    {
        _dbContext = dbContext;
        _estadoEnvioService = estadoEnvioService;
    }

    public async Task<IReadOnlyList<TransporteDto>> ObtenerMisTransportesAsync(int conductorId)
    {
        var asignaciones = await _dbContext.Asignaciones
            .AsNoTracking()
            .Include(a => a.Viaje)
            .Include(a => a.Envio)
            .Where(a => a.Viaje.UsuarioId == conductorId)
            .OrderByDescending(a => a.FechaAsignacion)
            .ToListAsync();

        return asignaciones.Select(Mapear).ToList();
    }

    public async Task<TransporteDto> ObtenerDetalleAsync(int asignacionId, int usuarioId)
    {
        var asignacion = await CargarAsignacionDelConductorAsync(asignacionId, usuarioId);
        return Mapear(asignacion);
    }

    public Task<TransporteDto> MarcarRetiradoAsync(int asignacionId, int usuarioId) =>
        CambiarEstadoAsync(asignacionId, usuarioId, EstadoEnvio.Retirado);

    public Task<TransporteDto> MarcarEnCaminoAsync(int asignacionId, int usuarioId) =>
        CambiarEstadoAsync(asignacionId, usuarioId, EstadoEnvio.EnCamino);

    public Task<TransporteDto> MarcarEntregadoAsync(int asignacionId, int usuarioId) =>
        CambiarEstadoAsync(asignacionId, usuarioId, EstadoEnvio.Entregado);

    private async Task<TransporteDto> CambiarEstadoAsync(int asignacionId, int usuarioId, EstadoEnvio nuevoEstado)
    {
        var asignacion = await CargarAsignacionDelConductorAsync(asignacionId, usuarioId);

        // Reglas de HU-09: lanza TransicionEstadoInvalidaException (409) si no corresponde.
        _estadoEnvioService.ValidarTransicion(asignacion.Envio.Estado, nuevoEstado);

        asignacion.Envio.Estado = nuevoEstado;
        await _dbContext.SaveChangesAsync(); // RowVersion del envío detecta cambios simultáneos (409)

        return Mapear(asignacion);
    }

    private async Task<Asignacion> CargarAsignacionDelConductorAsync(int asignacionId, int usuarioId)
    {
        var asignacion = await _dbContext.Asignaciones
            .Include(a => a.Viaje)
            .Include(a => a.Envio)
            .FirstOrDefaultAsync(a => a.Id == asignacionId)
            ?? throw new NotFoundException("El transporte indicado no existe.");

        // El remitente u otro usuario no pueden ver ni cambiar el seguimiento.
        if (asignacion.Viaje.UsuarioId != usuarioId)
            throw new AccesoDenegadoException("Solo el conductor asignado puede gestionar este transporte.");

        return asignacion;
    }

    private TransporteDto Mapear(Asignacion a) => new()
    {
        AsignacionId = a.Id,
        ViajeId = a.ViajeId,
        ViajeOrigen = a.Viaje.Origen,
        ViajeDestino = a.Viaje.Destino,
        EnvioId = a.EnvioId,
        Descripcion = a.Envio.Descripcion,
        PesoKg = a.Envio.PesoKg,
        Origen = a.Envio.Origen,
        Destino = a.Envio.Destino,
        Estado = a.Envio.Estado,
        Ganancia = a.Ganancia,
        PuedeMarcarRetirado = _estadoEnvioService.EstransaccionValida(a.Envio.Estado, EstadoEnvio.Retirado),
        PuedeMarcarEnCamino = _estadoEnvioService.EstransaccionValida(a.Envio.Estado, EstadoEnvio.EnCamino),
        PuedeMarcarEntregado = _estadoEnvioService.EstransaccionValida(a.Envio.Estado, EstadoEnvio.Entregado)
    };
}
