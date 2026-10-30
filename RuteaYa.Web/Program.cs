using Microsoft.AspNetCore.Authentication.Cookies;
using RuteaYa.Web.Servicios;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

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
