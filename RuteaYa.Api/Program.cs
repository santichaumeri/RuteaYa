using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RuteaYa.Api.Middleware;
using RuteaYa.Api.Services.Auth;
using RuteaYa.Api.Swagger;
using RuteaYa.Entidades;
using RuteaYa.Logica.Data;
using RuteaYa.Logica.Optimizacion;
using RuteaYa.Logica.Optimizacion.Criterios;
using RuteaYa.Logica.Servicios;
using RuteaYa.Logica.Servicios.Rutas;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Falta configurar ConnectionStrings:DefaultConnection en appsettings.json.");

builder.Services.AddDbContext<RuteaYaDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sql => sql.MigrationsAssembly("RuteaYa.Logica")));

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

// Servicios de negocio (viven en RuteaYa.Logica). Los controllers los reciben por inyección.
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IViajeService, ViajeService>();

// REGISTROS DE ENVÍOS (HU-07, HU-08, HU-09)
builder.Services.AddScoped<IEstadoEnvioService, EstadoEnvioService>();
builder.Services.AddScoped<IEnvioService, EnvioService>();

// OPTIMIZADOR (HU-13). El algoritmo no tiene estado, por eso es singleton.
builder.Services.AddSingleton<IOptimizadorEnvios, OptimizadorEnvios>();
builder.Services.AddScoped<IOptimizacionService, OptimizacionService>();

// CRITERIOS DE OPTIMIZACIÓN (HU-15, patrón Strategy). Un criterio nuevo = una clase + una línea acá.
builder.Services.AddSingleton<ICriterioOptimizacion>(CriterioMaximizarGanancias.Instancia);
builder.Services.AddSingleton<ICriterioOptimizacion, CriterioMinimizarDesvios>();
builder.Services.AddSingleton<ICriterioOptimizacion>(new CriterioEquilibrio(
    builder.Configuration.GetValue("Optimizacion:Equilibrio:CostoPorKm", CriterioEquilibrio.CostoPorKmPorDefecto)));
builder.Services.AddSingleton<ICatalogoCriterios, CatalogoCriterios>();

// OPORTUNIDADES (HU-11, HU-12). TimeProvider permite fijar "hoy" en los tests.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IOportunidadService, OportunidadService>();

// OPTIMIZADOR DEL VIAJE (HU-14): oportunidades de una fecha + optimizador + criterio.
builder.Services.AddScoped<IOptimizacionViajeService, OptimizacionViajeService>();

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

    // Botón "Authorize": se pega el token que devuelve POST api/usuarios/login (sin el prefijo "Bearer").
    options.AddSecurityDefinition(AutorizacionOperationFilter.Esquema, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Token JWT obtenido en POST api/usuarios/login."
    });
    options.OperationFilter<AutorizacionOperationFilter>();
});

var mapsApiKey = builder.Configuration["GoogleMaps:ApiKey"];
if (string.IsNullOrWhiteSpace(mapsApiKey))
{
    builder.Services.AddScoped<IRoutingService, MockRoutingService>();
}
else
{
    // Cuando se implemente el proveedor real:
    // builder.Services.AddScoped<IRoutingService, GoogleMapsRoutingService>();
    builder.Services.AddScoped<IRoutingService, MockRoutingService>();
}

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
