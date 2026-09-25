using AsadaSJM.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// BASE DE DATOS
// =========================================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection")));

// =========================================================
// AUTENTICACIÓN POR COOKIES
// =========================================================

builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";

        options.AccessDeniedPath = "/Account/AccesoDenegado";

        options.Cookie.Name = "AsadaSJM.Auth";

        options.Cookie.HttpOnly = true;

        options.ExpireTimeSpan =
            TimeSpan.FromHours(8);

        options.SlidingExpiration = true;
    });

// =========================================================
// AUTORIZACIÓN
// =========================================================

builder.Services.AddAuthorization();

// =========================================================
// MVC
// =========================================================

builder.Services.AddControllersWithViews();

var app = builder.Build();

// =========================================================
// CONFIGURACIÓN
// =========================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// IMPORTANTE: primero Authentication
app.UseAuthentication();

app.UseAuthorization();

// =========================================================
// RUTA PRINCIPAL
// =========================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Portal}/{action=Index}/{id?}");

app.Run();