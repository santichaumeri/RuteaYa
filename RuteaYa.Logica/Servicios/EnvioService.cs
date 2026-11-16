using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos.Envios;
using RuteaYa.Logica.Excepciones;

namespace RuteaYa.Logica.Servicios
{
    public class EnvioService : IEnvioService
    {
        private readonly RuteaYaDbContext _context;
        private readonly IEstadoEnvioService _estadoEnvioService;
        private readonly TimeProvider _reloj;

        /// <param name="reloj">Fuente de "hoy". Se inyecta para poder fijar la fecha en los tests.</param>
        public EnvioService(RuteaYaDbContext context, IEstadoEnvioService estadoEnvioService, TimeProvider? reloj = null)
        {
            _context = context;
            _estadoEnvioService = estadoEnvioService;
            _reloj = reloj ?? TimeProvider.System;
        }

        public async Task<EnvioDetalleDto> CrearEnvioAsync(CrearEnvioDto dto, int usuarioId)
        {
            // Validaciones de negocio defensivas a nivel servicio
            if (string.IsNullOrWhiteSpace(dto.Origen) || string.IsNullOrWhiteSpace(dto.Destino))
            {
                throw new ValidacionException("El origen y el destino son obligatorios.");
            }

            if (dto.Origen.Trim().Equals(dto.Destino.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidacionException("El origen y el destino no pueden ser iguales.");
            }

            if (dto.Fecha < DateOnly.FromDateTime(_reloj.GetLocalNow().DateTime))
            {
                throw new ValidacionException("La fecha no puede ser anterior a la actual.");
            }

            if (dto.PesoKg <= 0)
            {
                throw new ValidacionException("El peso debe ser mayor a 0.");
            }

            if (dto.MontoOfrecido <= 0)
            {
                throw new ValidacionException("El monto ofrecido debe ser mayor a 0.");
            }

            var nuevoEnvio = new Envio
            {
                UsuarioId = usuarioId,
                Origen = dto.Origen.Trim(),
                Destino = dto.Destino.Trim(),
                Fecha = dto.Fecha,
                PesoKg = dto.PesoKg,
                Tamano = dto.Tamano,
                Descripcion = dto.Descripcion.Trim(),
                MontoOfrecido = dto.MontoOfrecido,
                Estado = EstadoEnvio.BuscandoConductor // Estado inicial obligatorio
            };

            _context.Envios.Add(nuevoEnvio);
            await _context.SaveChangesAsync();

            return new EnvioDetalleDto
            {
                Id = nuevoEnvio.Id,
                Origen = nuevoEnvio.Origen,
                Destino = nuevoEnvio.Destino,
                Fecha = nuevoEnvio.Fecha,
                PesoKg = nuevoEnvio.PesoKg,
                Tamano = nuevoEnvio.Tamano,
                Descripcion = nuevoEnvio.Descripcion,
                MontoOfrecido = nuevoEnvio.MontoOfrecido,
                Estado = nuevoEnvio.Estado
            };
        }

        public async Task<IEnumerable<EnvioDetalleDto>> ObtenerMisEnviosAsync(int UsuarioId, EstadoEnvio? estado = null)
        {
            var query = ConViajeAsociado(_context.Envios.AsNoTracking().Where(e=> e.UsuarioId == UsuarioId));

            if(estado.HasValue)
            {
                query = query.Where(e => e.Estado == estado.Value);
            }

            var envios = await query
                .OrderByDescending(e => e.Fecha)
                .ToListAsync();

            return envios.Select(ADetalle).ToList();
        }

        public async Task<EnvioDetalleDto?> ObtenerPorIdAsync(int id, int usuarioId)
        {
            var envio = await ConViajeAsociado(_context.Envios.AsNoTracking())
                .FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == usuarioId);

            return envio == null ? null : ADetalle(envio);
        }

        /// <summary>Trae el viaje que lleva el envío (si ya tiene conductor), con su conductor y sus paradas.</summary>
        private static IQueryable<Envio> ConViajeAsociado(IQueryable<Envio> query) => query
            .Include(e => e.Asignacion!).ThenInclude(a => a.Viaje).ThenInclude(v => v.Usuario)
            .Include(e => e.Asignacion!).ThenInclude(a => a.Viaje).ThenInclude(v => v.Paradas);

        private static EnvioDetalleDto ADetalle(Envio envio) => new()
        {
            Id = envio.Id,
            Origen = envio.Origen,
            Destino = envio.Destino,
            Fecha = envio.Fecha,
            PesoKg = envio.PesoKg,
            Tamano = envio.Tamano,
            Descripcion = envio.Descripcion,
            MontoOfrecido = envio.MontoOfrecido,
            Estado = envio.Estado,
            ViajeAsociado = envio.Asignacion is null ? null : AViajeAsociado(envio.Asignacion)
        };

        private static ViajeAsociadoDto AViajeAsociado(Asignacion asignacion) => new()
        {
            ViajeId = asignacion.ViajeId,
            Conductor = $"{asignacion.Viaje.Usuario.Nombre} {asignacion.Viaje.Usuario.Apellido}".Trim(),
            Origen = asignacion.Viaje.Origen,
            Destino = asignacion.Viaje.Destino,
            HoraSalida = asignacion.Viaje.HoraSalida.ToTimeSpan(),
            HoraLlegada = asignacion.Viaje.HoraLlegada.ToTimeSpan(),
            Paradas = asignacion.Viaje.Paradas
                .OrderBy(p => p.Orden)
                .Select(p => p.Direccion)
                .ToList(),
            FechaAsignacion = asignacion.FechaAsignacion
        };

        public async Task CancelarEnvioAsync(int id, int usuarioId)
        {
            var envio = await _context.Envios
                .FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == usuarioId);

            if( envio == null) 
            { 
                throw new NotFoundException($"No se encontró el envío con Id {id} para el usuario autenticado"); 
            }

            // Valida la transición usando la máquina de estados de la HU-09 (409 si no corresponde)
            _estadoEnvioService.ValidarTransicion(envio.Estado, EstadoEnvio.Cancelado);

            envio.Estado = EstadoEnvio.Cancelado;
            await _context.SaveChangesAsync();
        }

        
    }
}
