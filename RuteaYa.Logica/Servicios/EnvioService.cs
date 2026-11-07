using Microsoft.EntityFrameworkCore;
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
