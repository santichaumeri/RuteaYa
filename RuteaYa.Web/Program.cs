using Microsoft.AspNetCore.Authentication.Cookies;
using RuteaYa.Logica.Servicios;
using RuteaYa.Web.Servicios;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    var mensajes = options.ModelBindingMessageProvider;

    mensajes.SetValueMustNotBeNullAccessor(
        campo => $"El campo {campo} es obligatorio.");
    mensajes.SetValueMustBeANumberAccessor(
        campo => $"El valor ingresado no es válido para {campo}.");
    mensajes.SetAttemptedValueIsInvalidAccessor(
        (valor, campo) => $"El valor '{valor}' no es válido para {campo}.");
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<DashboardApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!);
});

builder.Services.AddHttpClient<IViajesApiClient, ViajesApiClient>(client =>
{
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
        ?? throw new InvalidOperationException("Falta la configuración ApiBaseUrl.");
    client.BaseAddress = new Uri(apiBaseUrl);
});

// Cliente HTTP tipado hacia la API. La URL base sale de "ApiBaseUrl" en appsettings.json.
builder.Services.AddHttpClient<IUsuariosApiClient, UsuariosApiClient>(client =>
{
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
        ?? throw new InvalidOperationException("Falta la configuración ApiBaseUrl.");
    client.BaseAddress = new Uri(apiBaseUrl);
});

// Sesión por cookie: el MVC la crea con los claims del usuario que valida la API.
// Las páginas con [Authorize] redirigen a LoginPath cuando no hay sesión.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddHttpClient<IEnviosApiClient, EnviosApiClient>(client =>
{
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
        ?? throw new InvalidOperationException("Falta la configuración ApiBaseUrl.");
    client.BaseAddress = new Uri(apiBaseUrl);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();   // lee la cookie y arma el User; va antes de UseAuthorization
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
