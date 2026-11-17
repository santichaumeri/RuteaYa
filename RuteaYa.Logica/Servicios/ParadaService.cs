using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos;
using RuteaYa.Logica.Excepciones;

namespace RuteaYa.Logica.Servicios;

public interface IParadaService
{
    /// <summary>Paradas del viaje en el orden del recorrido.</summary>
    Task<IReadOnlyList<ParadaResponse>> ObtenerAsync(int viajeId, int usuarioId, CancellationToken cancellationToken);

    /// <summary>Agrega una parada en la posición pedida (o al final) y corre las siguientes.</summary>
    Task<ParadaResponse> CrearAsync(int viajeId, int usuarioId, ParadaRequest request, CancellationToken cancellationToken);

    /// <summary>Cambia la dirección de una parada y, si se indica, la mueve a otra posición.</summary>
    Task<ParadaResponse> ModificarAsync(int viajeId, int paradaId, int usuarioId, ParadaRequest request, CancellationToken cancellationToken);

    /// <summary>Elimina una parada y renumera las demás para que el orden siga siendo 1, 2, 3...</summary>
    Task EliminarAsync(int viajeId, int paradaId, int usuarioId, CancellationToken cancellationToken);
}

/// <summary>
/// Paradas intermedias de un viaje como recurso propio de la API (§9: "Paradas: crear/modificar/eliminar
/// dentro de un viaje"). Aplica las mismas reglas que el alta y la edición del viaje (<see cref="ReglasParadas"/>).
/// </summary>
public sealed class ParadaService(RuteaYaDbContext dbContext) : IParadaService
{
    public async Task<IReadOnlyList<ParadaResponse>> ObtenerAsync(int viajeId, int usuarioId, CancellationToken cancellationToken)
    {
        var viaje = await ObtenerViajePropioAsync(viajeId, usuarioId, cancellationToken);
        return viaje.Paradas.OrderBy(x => x.Orden).Select(AResponse).ToList();
    }

    public async Task<ParadaResponse> CrearAsync(int viajeId, int usuarioId, ParadaRequest request, CancellationToken cancellationToken)
    {
        var viaje = await ObtenerViajePropioAsync(viajeId, usuarioId, cancellationToken);
        var ordenadas = viaje.Paradas.OrderBy(x => x.Orden).ToList();

        var posicion = request.Orden ?? ordenadas.Count + 1;
        ValidarPosicion(posicion, ordenadas.Count + 1);

        var nueva = new Parada { ViajeId = viaje.Id, Direccion = Normalizar(request.Direccion) };
        ordenadas.Insert(posicion - 1, nueva);
        ReglasParadas.Validar(ordenadas.Select(x => x.Direccion).ToList(), viaje.Origen, viaje.Destino);

        viaje.Paradas.Add(nueva);
        await GuardarOrdenAsync(ordenadas, cancellationToken);

        return AResponse(nueva);
    }

    public async Task<ParadaResponse> ModificarAsync(
        int viajeId, int paradaId, int usuarioId, ParadaRequest request, CancellationToken cancellationToken)
    {
        var viaje = await ObtenerViajePropioAsync(viajeId, usuarioId, cancellationToken);
        var ordenadas = viaje.Paradas.OrderBy(x => x.Orden).ToList();
        var parada = ObtenerParada(ordenadas, paradaId);

        var posicion = request.Orden ?? ordenadas.IndexOf(parada) + 1;
        ValidarPosicion(posicion, ordenadas.Count);

        ordenadas.Remove(parada);
        ordenadas.Insert(posicion - 1, parada);
        var direccion = Normalizar(request.Direccion);
        ReglasParadas.Validar(
            ordenadas.Select(x => x == parada ? direccion : x.Direccion).ToList(),
            viaje.Origen,
            viaje.Destino);

        parada.Direccion = direccion;
        await GuardarOrdenAsync(ordenadas, cancellationToken);

        return AResponse(parada);
    }

    public async Task EliminarAsync(int viajeId, int paradaId, int usuarioId, CancellationToken cancellationToken)
    {
        var viaje = await ObtenerViajePropioAsync(viajeId, usuarioId, cancellationToken);
        var ordenadas = viaje.Paradas.OrderBy(x => x.Orden).ToList();
        var parada = ObtenerParada(ordenadas, paradaId);

        ordenadas.Remove(parada);
        dbContext.Paradas.Remove(parada);
        await GuardarOrdenAsync(ordenadas, cancellationToken);
    }

    /// <summary>
    /// Deja las paradas numeradas 1, 2, 3... en el orden recibido. La base tiene un índice único
    /// (ViajeId, Orden), así que primero se pasan a números negativos y después a los definitivos:
    /// si se actualizaran directo, mover una parada chocaría con el orden de otra a mitad de camino.
    /// </summary>
    private async Task GuardarOrdenAsync(IReadOnlyList<Parada> ordenadas, CancellationToken cancellationToken)
    {
        await using var transaccion = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        for (var i = 0; i < ordenadas.Count; i++)
        {
            ordenadas[i].Orden = -(i + 1);
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        for (var i = 0; i < ordenadas.Count; i++)
        {
            ordenadas[i].Orden = i + 1;
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        await transaccion.CommitAsync(cancellationToken);
    }

    private async Task<Viaje> ObtenerViajePropioAsync(int viajeId, int usuarioId, CancellationToken cancellationToken)
    {
        var viaje = await dbContext.Viajes
            .Include(x => x.Paradas)
            .SingleOrDefaultAsync(x => x.Id == viajeId, cancellationToken)
            ?? throw new NotFoundException("El viaje no existe.");

        if (viaje.UsuarioId != usuarioId)
        {
            throw new AccesoDenegadoException("No tenés permiso para modificar este viaje.");
        }

        return viaje;
    }

    private static Parada ObtenerParada(IEnumerable<Parada> paradas, int paradaId) =>
        paradas.SingleOrDefault(x => x.Id == paradaId)
        ?? throw new NotFoundException("La parada no existe en este viaje.");

    private static void ValidarPosicion(int posicion, int maxima)
    {
        if (posicion < 1 || posicion > maxima)
        {
            throw new ValidacionException($"El orden de la parada tiene que estar entre 1 y {maxima}.");
        }
    }

    private static string Normalizar(string? direccion) => direccion?.Trim() ?? string.Empty;

    private static ParadaResponse AResponse(Parada parada) => new()
    {
        Id = parada.Id,
        Direccion = parada.Direccion,
        Orden = parada.Orden
    };
}
