using RuteaYa.Logica.Dtos.Asignaciones;
using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.Logica.Servicios;

public interface IAsignacionService
{
    Task<AsignacionDetalleDto> AceptarOportunidadAsync(int usuarioId, AsignarOportunidadRequestDto dto);
}
