using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos.Envios;
using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.Logica.Servicios
{
    public class EnvioService : IEnvioService
    {
        private readonly RuteaYaDbContext _context;
        private readonly IEstadoEnvioService _estadoEnvioService;

        public EnvioService(RuteaYaDbContext context, IEstadoEnvioService estadoEnvioService)
        {
            _context = context;
            _estadoEnvioService = estadoEnvioService;
        }

        public async Task<EnvioDetalleDto> CrearEnvioAsync(CrearEnvioDto dto, int usuarioId)
        {
            // Validaciones de negocio defensivas a nivel servicio
            if (string.IsNullOrWhiteSpace(dto.Origen) || string.IsNullOrWhiteSpace(dto.Destino))
            {
                throw new ArgumentException("El origen y el destino son obligatorios.");
            }

            if (dto.Origen.Trim().Equals(dto.Destino.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("El origen y el destino no pueden ser iguales.");
            }

            if (dto.Fecha < DateOnly.FromDateTime(DateTime.Today))
            {
                throw new ArgumentException("La fecha no puede ser anterior a la actual.");
            }

            if (dto.PesoKg <= 0)
            {
                throw new ArgumentException("El peso debe ser mayor a 0.");
            }

            if (dto.MontoOfrecido <= 0)
            {
                throw new ArgumentException("El monto ofrecido debe ser mayor a 0.");
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
            var query = _context.Envios.AsNoTracking().Where(e=> e.UsuarioId == UsuarioId);

            if(estado.HasValue)
            {
                query = query.Where(e => e.Estado == estado.Value);
            }

            return await query
                .OrderByDescending(e => e.Fecha)
                .Select(e => new EnvioDetalleDto
                {
                    Id = e.Id,
                    Origen = e.Origen,
                    Destino = e.Destino,
                    Fecha = e.Fecha,
                    PesoKg = e.PesoKg,
                    Tamano = e.Tamano,
                    Descripcion = e.Descripcion,
                    MontoOfrecido = e.MontoOfrecido,
                    Estado = e.Estado,
                }).ToListAsync();   
        }

        public async Task<EnvioDetalleDto?> ObtenerPorIdAsync(int id, int usuarioId)
        {
            var envio = await _context.Envios
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == usuarioId);

            if(envio == null)
            {
                return null;
            }

            return new EnvioDetalleDto
            {
                Id = envio.Id,
                Origen = envio.Origen,
                Destino = envio.Destino,
                Fecha = envio.Fecha,
                PesoKg = envio.PesoKg,
                Tamano = envio.Tamano,
                Descripcion = envio.Descripcion,
                MontoOfrecido = envio.MontoOfrecido,
                Estado = envio.Estado
            };

        }

        public async Task CancelarEnvioAsync(int id, int usuarioId)
        {
            var envio = await _context.Envios
                .FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == usuarioId);

            if( envio == null) 
            { 
                throw new KeyNotFoundException($"No se encontró el envío con Id {id} para el usuario autenticado"); 
            }

            // Valida la transición usando la máquina de estados de la HU-09
            _estadoEnvioService.ValidarTransicion(envio.Estado, EstadoEnvio.Cancelado);

            envio.Estado = EstadoEnvio.Cancelado;
            await _context.SaveChangesAsync();
        }

        
    }
}
