using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos;
using RuteaYa.Logica.Excepciones;

namespace RuteaYa.Logica.Servicios;

public sealed class ViajeService(RuteaYaDbContext dbContext) : IViajeService
{
    private const DiasSemana DiasSemanaValidos =
        DiasSemana.Lunes
        | DiasSemana.Martes
        | DiasSemana.Miercoles
        | DiasSemana.Jueves
        | DiasSemana.Viernes
        | DiasSemana.Sabado
        | DiasSemana.Domingo;

    public async Task<ViajeResponse> CrearViajeAsync(
        int usuarioId,
        CrearViajeRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ValidacionException("Los datos del viaje son obligatorios.");
        }

        var origen = NormalizarDireccion(request.Origen);
        var destino = NormalizarDireccion(request.Destino);

        ValidarDatosGenerales(request, origen, destino);

        var paradas = (request.Paradas ?? [])
            .Select(parada => NormalizarDireccion(parada?.Direccion ?? string.Empty))
            .ToList();

        ValidarParadas(paradas, origen, destino);

        var viaje = new Viaje
        {
            UsuarioId = usuarioId,
            Origen = origen,
            Destino = destino,
            DiasSemana = request.DiasSemana,
            HoraSalida = TimeOnly.FromTimeSpan(request.HoraSalida),
            HoraLlegada = TimeOnly.FromTimeSpan(request.HoraLlegada),
            MaxPaquetes = request.MaxPaquetes,
            PesoMaximoKg = request.PesoMaximoKg,
            DesvioMaximoKm = request.DesvioMaximoKm,
            Estado = EstadoViaje.Activo,
            Paradas = paradas
                .Select((direccion, indice) => new Parada
                {
                    Direccion = direccion,
                    Orden = indice + 1
                })
                .ToList()
        };

        dbContext.Viajes.Add(viaje);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AResponse(viaje);
    }

    private static void ValidarDatosGenerales(
        CrearViajeRequest request,
        string origen,
        string destino)
    {
        if (origen.Length == 0)
        {
            throw new ValidacionException("El origen es obligatorio.");
        }

        if (destino.Length == 0)
        {
            throw new ValidacionException("El destino es obligatorio.");
        }

        if (string.Equals(origen, destino, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidacionException("El origen y el destino deben ser diferentes.");
        }

        if (request.DiasSemana == DiasSemana.Ninguno
            || (request.DiasSemana & ~DiasSemanaValidos) != DiasSemana.Ninguno)
        {
            throw new ValidacionException("Debe seleccionar al menos un día de la semana válido.");
        }

        if (!EsHoraValida(request.HoraSalida))
        {
            throw new ValidacionException("La hora de salida no es válida.");
        }

        if (!EsHoraValida(request.HoraLlegada))
        {
            throw new ValidacionException("La hora de llegada no es válida.");
        }

        if (request.MaxPaquetes <= 0)
        {
            throw new ValidacionException("La cantidad máxima de paquetes debe ser mayor a cero.");
        }

        if (request.PesoMaximoKg <= 0)
        {
            throw new ValidacionException("El peso máximo debe ser mayor a cero.");
        }

        if (request.DesvioMaximoKm <= 0)
        {
            throw new ValidacionException("El desvío máximo debe ser mayor a cero.");
        }
    }

    private static void ValidarParadas(
        IReadOnlyCollection<string> paradas,
        string origen,
        string destino)
    {
        var direcciones = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var parada in paradas)
        {
            if (parada.Length == 0)
            {
                throw new ValidacionException("La dirección de cada parada es obligatoria.");
            }

            if (string.Equals(parada, origen, StringComparison.OrdinalIgnoreCase)
                || string.Equals(parada, destino, StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidacionException(
                    "Una parada no puede coincidir con el origen ni con el destino.");
            }

            if (!direcciones.Add(parada))
            {
                throw new ValidacionException("No se pueden repetir direcciones de paradas.");
            }
        }
    }

    private static bool EsHoraValida(TimeSpan hora) =>
        hora >= TimeSpan.Zero && hora < TimeSpan.FromDays(1);

    private static string NormalizarDireccion(string direccion) =>
        direccion?.Trim() ?? string.Empty;

    private static ViajeResponse AResponse(Viaje viaje) => new()
    {
        Id = viaje.Id,
        UsuarioId = viaje.UsuarioId,
        Origen = viaje.Origen,
        Destino = viaje.Destino,
        DiasSemana = viaje.DiasSemana,
        HoraSalida = viaje.HoraSalida.ToTimeSpan(),
        HoraLlegada = viaje.HoraLlegada.ToTimeSpan(),
        MaxPaquetes = viaje.MaxPaquetes,
        PesoMaximoKg = viaje.PesoMaximoKg,
        DesvioMaximoKm = viaje.DesvioMaximoKm,
        Estado = viaje.Estado,
        Paradas = viaje.Paradas
            .OrderBy(parada => parada.Orden)
            .Select(parada => new ParadaResponse
            {
                Id = parada.Id,
                Direccion = parada.Direccion,
                Orden = parada.Orden
            })
            .ToList()
    };
}
