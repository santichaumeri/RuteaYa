using RuteaYa.Logica.Dtos.Asignaciones;
using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.Logica.Servicios;

public interface IAsignacionService
{
    Task<AsignacionDetalleDto> AceptarOportunidadAsync(int usuarioId, AsignarOportunidadRequestDto dto);

    /// <summary>
    /// Reserva de una vez todos los envíos de una combinación (HU-17). Es atómico: se asignan todos o ninguno.
    /// Lanza CombinacionConflictoException (409) con los ids si alguno ya no está disponible,
    /// ConflictException si se exceden cupo, peso o desvío, y NotFound/AccesoDenegado/Validacion según el caso.
    /// </summary>
    Task<CombinacionConfirmadaDto> ConfirmarCombinacionAsync(int usuarioId, ConfirmarCombinacionRequestDto dto);
}
