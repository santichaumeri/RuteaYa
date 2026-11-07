using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Dtos.Envios;
using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.Logica.Servicios;

public interface IEnvioService
{
    Task<IEnumerable<EnvioDetalleDto>> ObtenerMisEnviosAsync(int UsuarioId, EstadoEnvio? estado = null);
    Task<EnvioDetalleDto?> ObtenerPorIdAsync(int id, int usuarioId);
    Task CancelarEnvioAsync(int id, int usuarioId);
}
