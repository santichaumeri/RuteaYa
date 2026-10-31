using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RuteaYa.Api.Middleware;
using RuteaYa.Entidades;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Servicios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<RuteaYaDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.MigrationsAssembly("RuteaYa.Logica")));

// Servicios de negocio (viven en RuteaYa.Logica). Los controllers los reciben por inyección.
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();

// Manejo global de errores (400, 401, 403, 404, 409, 500)
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorGlobalDeExcepciones>();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Incluye los comentarios /// de los controllers en la documentacion
    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

app.UseExceptionHandler();   // tiene que ir primero en el pipeline

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
