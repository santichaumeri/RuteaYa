using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using RuteaYa.Api.Middleware;
using RuteaYa.Entidades;
using RuteaYa.Api.Services.Auth;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Servicios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Falta configurar Jwt:Key.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Falta configurar Jwt:Issuer.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Falta configurar Jwt:Audience.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IViajeService, ViajeService>();

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

// En desarrollo, aplica las migraciones pendientes al iniciar (crea la base si no existe).
// Así después de un git pull no hace falta correr "dotnet ef database update" a mano.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RuteaYaDbContext>();
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler();   // tiene que ir primero en el pipeline

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
