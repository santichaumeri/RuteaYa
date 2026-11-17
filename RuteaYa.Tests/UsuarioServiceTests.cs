using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Dtos;
using RuteaYa.Logica.Excepciones;
using RuteaYa.Logica.Servicios;
using Xunit;

namespace RuteaYa.Tests;

/// <summary>Registro y login (pantalla 1, endpoints api/usuarios/registro y api/usuarios/login).</summary>
public sealed class UsuarioServiceTests
{
    private const string Password = "Clave-Segura1";

    [Fact]
    public async Task Registrar_guarda_el_usuario_con_la_contrasena_hasheada_y_el_email_normalizado()
    {
        await using var db = CrearDbContext();
        var hasher = new PasswordHasher<Usuario>();

        var respuesta = await new UsuarioService(db, hasher).RegistrarAsync(Registro("  Ana.Lopez@Example.com "), CancellationToken.None);

        var guardado = await db.Usuarios.SingleAsync();
        Assert.Equal("ana.lopez@example.com", guardado.Email);
        Assert.Equal("ana.lopez@example.com", respuesta.Email);
        Assert.Equal("Ana", guardado.Nombre);
        Assert.NotEqual(Password, guardado.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(guardado, guardado.PasswordHash, Password));
    }

    [Fact]
    public async Task Registrar_rechaza_un_email_ya_registrado_aunque_cambien_mayusculas()
    {
        await using var db = CrearDbContext();
        var servicio = new UsuarioService(db, new PasswordHasher<Usuario>());
        await servicio.RegistrarAsync(Registro("ana@example.com"), CancellationToken.None);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            servicio.RegistrarAsync(Registro("ANA@example.com"), CancellationToken.None));

        Assert.Equal("Ya existe un usuario registrado con ese email.", error.Message);
        Assert.Equal(1, await db.Usuarios.CountAsync());
    }

    [Fact]
    public async Task Login_con_credenciales_correctas_devuelve_el_usuario()
    {
        await using var db = CrearDbContext();
        var servicio = new UsuarioService(db, new PasswordHasher<Usuario>());
        await servicio.RegistrarAsync(Registro("ana@example.com"), CancellationToken.None);

        var usuario = await servicio.LoginAsync(
            new LoginRequest { Email = " ANA@example.com", Password = Password },
            CancellationToken.None);

        Assert.Equal("ana@example.com", usuario.Email);
        Assert.Equal("López", usuario.Apellido);
    }

    [Fact]
    public async Task Login_fallido_da_el_mismo_mensaje_si_el_email_no_existe_o_la_contrasena_es_incorrecta()
    {
        await using var db = CrearDbContext();
        var servicio = new UsuarioService(db, new PasswordHasher<Usuario>());
        await servicio.RegistrarAsync(Registro("ana@example.com"), CancellationToken.None);

        var contrasenaIncorrecta = await Assert.ThrowsAsync<NoAutenticadoException>(() => servicio.LoginAsync(
            new LoginRequest { Email = "ana@example.com", Password = "otra-clave" }, CancellationToken.None));
        var emailInexistente = await Assert.ThrowsAsync<NoAutenticadoException>(() => servicio.LoginAsync(
            new LoginRequest { Email = "nadie@example.com", Password = Password }, CancellationToken.None));

        // Mismo mensaje en los dos casos: no revela si el email está registrado.
        Assert.Equal(contrasenaIncorrecta.Message, emailInexistente.Message);
    }

    private static RegistroRequest Registro(string email) => new()
    {
        Nombre = " Ana ",
        Apellido = "López",
        Email = email,
        Password = Password,
        ConfirmarPassword = Password
    };

    private static RuteaYaDbContext CrearDbContext() =>
        new(new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
