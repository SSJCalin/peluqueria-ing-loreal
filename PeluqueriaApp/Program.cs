using Microsoft.EntityFrameworkCore;
using PeluqueriaApp.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<PeluqueriaApp.Datos.ContextoBD>(opciones =>
    opciones.UseSqlite("Data Source=peluqueria.db"));
    builder.Services.AddScoped<PeluqueriaApp.Servicios.ServicioCitas>();
    builder.Services.AddScoped<PeluqueriaApp.Servicios.ServicioAutenticacion>();
    builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.LoginPath = "/login";
        opciones.AccessDeniedPath = "/login";
        opciones.ExpireTimeSpan = TimeSpan.FromDays(30);
        opciones.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var bd = scope.ServiceProvider.GetRequiredService<PeluqueriaApp.Datos.ContextoBD>();
        PeluqueriaApp.Datos.Sembrador.Sembrar(bd, builder.Configuration);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
